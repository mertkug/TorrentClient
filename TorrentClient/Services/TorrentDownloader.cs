using System.Security.Cryptography;
using TorrentClient.Models;
using TorrentClient.Protocol.Messages;
using TorrentClient.Tcp;

namespace TorrentClient.Services;

/// <summary>
/// Manages downloading a torrent - tracks pieces, writes to disk, verifies hashes
/// </summary>
public class TorrentDownloader
{
    private readonly Torrent _torrent;
    private readonly ILogger<TorrentDownloader> _logger;
    private readonly string _outputPath;
    
    // Download state
    private readonly byte[][] _pieces;           // Downloaded piece data
    private readonly bool[] _completedPieces;    // Which pieces are verified
    private readonly object _lock = new();
    
    // Progress tracking
    private int _currentPieceIndex = 0;
    private int _currentBlockOffset = 0;
    
    // Pipelining - send multiple requests at once
    private const int MaxPendingRequests = 25;
    private readonly HashSet<(int pieceIndex, int blockOffset)> _pendingRequests = new();
    
    // Track which blocks we've received for each piece (for out-of-order handling)
    private readonly Dictionary<int, HashSet<int>> _receivedBlocks = new();
    
    // Signal download completion to stop message loop
    private readonly CancellationTokenSource _completionCts = new();
    
    public int TotalPieces { get; }
    public int CompletedPieceCount => _completedPieces.Count(x => x);
    public long PieceLength => _torrent.Info.PieceLength;
    public int BlockSize => RequestMessage.DefaultBlockSize; // 16KB
    public bool IsComplete => CompletedPieceCount == TotalPieces;
    public CancellationToken CompletionToken => _completionCts.Token;

    public TorrentDownloader(Torrent torrent, string outputDirectory, ILogger<TorrentDownloader> logger)
    {
        _torrent = torrent;
        _logger = logger;
        _outputPath = Path.Combine(outputDirectory, torrent.Info.Name ?? "download");
        
        // Calculate total pieces
        TotalPieces = (int)Math.Ceiling((double)torrent.Info.Length / torrent.Info.PieceLength);
        _pieces = new byte[TotalPieces][];
        _completedPieces = new bool[TotalPieces];
        
        _logger.LogInformation("TorrentDownloader initialized: {Name}, {Pieces} pieces, {Length} bytes",
            torrent.Info.Name, TotalPieces, torrent.Info.Length);
    }

    /// <summary>
    /// Handle incoming peer message - updates state and returns next action
    /// </summary>
    public async Task HandleMessageAsync(PeerConnection connection, IPeerMessage message)
    {
        switch (message)
        {
            case BitfieldMessage bitfield:
                _logger.LogInformation("Peer has {Count} pieces", 
                    Enumerable.Range(0, TotalPieces).Count(i => bitfield.HasPiece(i)));
                await connection.SendInterestedAsync();
                break;

            case UnchokeMessage:
                _logger.LogInformation("Peer unchoked us - starting download with pipelining");
                if (!connection.AmInterested)
                    await connection.SendInterestedAsync();
                
                // Clear any stale pending requests from previous peer
                _pendingRequests.Clear();
                
                // Fill the pipeline with initial requests
                for (int i = 0; i < MaxPendingRequests; i++)
                {
                    await RequestNextBlockAsync(connection);
                }
                break;

            case PieceMessage piece:
                await HandlePieceAsync(connection, piece);
                break;

            case ChokeMessage:
                _logger.LogWarning("Peer choked us - waiting for unchoke");
                break;
        }
    }

    private async Task HandlePieceAsync(PeerConnection connection, PieceMessage piece)
    {
        // Remove from pending requests
        _pendingRequests.Remove((piece.Index, piece.Begin));
        
        lock (_lock)
        {
            // Initialize piece buffer if needed
            var pieceSize = GetPieceSize(piece.Index);
            if (_pieces[piece.Index] == null)
                _pieces[piece.Index] = new byte[pieceSize];

            // Copy block data into piece
            Buffer.BlockCopy(piece.Block, 0, _pieces[piece.Index], piece.Begin, piece.Block.Length);
        }

        _logger.LogDebug("Received: piece {Index}, offset {Offset}, {Bytes} bytes (pending: {Pending})",
            piece.Index, piece.Begin, piece.Block.Length, _pendingRequests.Count);

        // Track this block as received
        if (!_receivedBlocks.ContainsKey(piece.Index))
            _receivedBlocks[piece.Index] = new HashSet<int>();
        _receivedBlocks[piece.Index].Add(piece.Begin);
        
        // Check if ALL blocks for this piece have been received
        var expectedBlockCount = (int)Math.Ceiling((double)GetPieceSize(piece.Index) / BlockSize);
        var pieceComplete = _receivedBlocks[piece.Index].Count >= expectedBlockCount;

        if (pieceComplete)
        {
            if (VerifyPiece(piece.Index))
            {
                _completedPieces[piece.Index] = true;
                _receivedBlocks.Remove(piece.Index); // Clean up tracking
                _logger.LogInformation("✓ Piece {Index} complete and verified ({Completed}/{Total})",
                    piece.Index, CompletedPieceCount, TotalPieces);

                // Write piece to disk
                await WritePieceToDiskAsync(piece.Index);

                // Move to next piece
                _currentPieceIndex = FindNextNeededPiece();
                _currentBlockOffset = 0;
            }
            else
            {
                _logger.LogWarning("✗ Piece {Index} failed verification - will re-download", piece.Index);
                _pieces[piece.Index] = null; // Clear and retry
                _receivedBlocks.Remove(piece.Index); // Clear received blocks tracking
                _currentBlockOffset = 0;
            }
        }

        // Request next block if not complete
        if (!IsComplete)
        {
            await RequestNextBlockAsync(connection);
        }
        else
        {
            _logger.LogInformation("🎉 Download complete! File saved to: {Path}", _outputPath);
            _completionCts.Cancel(); // Signal completion to stop message loop
        }
    }

    private async Task RequestNextBlockAsync(PeerConnection connection)
    {
        // Check if pipeline is full
        if (_pendingRequests.Count >= MaxPendingRequests)
            return;
        
        // Get next block to request
        var (pieceIndex, blockOffset) = GetNextBlockToRequest();
        if (pieceIndex < 0)
            return; // Nothing left to request
        
        // Track and send request
        _pendingRequests.Add((pieceIndex, blockOffset));
        var blockLength = Math.Min(BlockSize, GetPieceSize(pieceIndex) - blockOffset);
        await connection.SendRequestAsync(pieceIndex, blockOffset, blockLength);
    }
    
    private (int pieceIndex, int blockOffset) GetNextBlockToRequest()
    {
        // Skip completed pieces and already-requested blocks
        while (_currentPieceIndex < TotalPieces)
        {
            // Skip completed pieces
            if (_completedPieces[_currentPieceIndex])
            {
                _currentPieceIndex++;
                _currentBlockOffset = 0;
                continue;
            }
            
            // Check if this block is already pending
            if (_pendingRequests.Contains((_currentPieceIndex, _currentBlockOffset)))
            {
                // Move to next block
                _currentBlockOffset += BlockSize;
                if (_currentBlockOffset >= GetPieceSize(_currentPieceIndex))
                {
                    _currentPieceIndex++;
                    _currentBlockOffset = 0;
                }
                continue;
            }
            
            // Found a block to request
            var result = (_currentPieceIndex, _currentBlockOffset);
            
            // Advance for next call
            _currentBlockOffset += BlockSize;
            if (_currentBlockOffset >= GetPieceSize(_currentPieceIndex))
            {
                _currentPieceIndex++;
                _currentBlockOffset = 0;
            }
            
            return result;
        }
        
        return (-1, -1); // Nothing left
    }

    private int GetPieceSize(int pieceIndex)
    {
        // Last piece may be smaller
        if (pieceIndex == TotalPieces - 1)
        {
            var remainder = (int)(_torrent.Info.Length % _torrent.Info.PieceLength);
            return remainder > 0 ? remainder : (int)_torrent.Info.PieceLength;
        }
        return (int)_torrent.Info.PieceLength;
    }

    private bool VerifyPiece(int pieceIndex)
    {
        var pieceData = _pieces[pieceIndex];
        if (pieceData == null) return false;

        // Get expected hash (20 bytes per piece)
        var expectedHash = _torrent.Info.Pieces.AsSpan(pieceIndex * 20, 20);
        
        // Calculate actual hash
        var actualHash = SHA1.HashData(pieceData);
        
        return expectedHash.SequenceEqual(actualHash);
    }

    private int FindNextNeededPiece()
    {
        for (int i = 0; i < TotalPieces; i++)
        {
            if (!_completedPieces[i])
                return i;
        }
        return TotalPieces; // All done
    }

    private async Task WritePieceToDiskAsync(int pieceIndex)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Calculate file offset for this piece
        var offset = pieceIndex * _torrent.Info.PieceLength;
        
        await using var fs = new FileStream(_outputPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        fs.Seek(offset, SeekOrigin.Begin);
        await fs.WriteAsync(_pieces[pieceIndex]);
        
        // Free memory after writing
        _pieces[pieceIndex] = null!;
    }
}
