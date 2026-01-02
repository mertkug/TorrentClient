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
    
    public int TotalPieces { get; }
    public int CompletedPieceCount => _completedPieces.Count(x => x);
    public long PieceLength => _torrent.Info.PieceLength;
    public int BlockSize => RequestMessage.DefaultBlockSize; // 16KB
    public bool IsComplete => CompletedPieceCount == TotalPieces;

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
                _logger.LogInformation("Peer unchoked us - starting download");
                if (!connection.AmInterested)
                    await connection.SendInterestedAsync();
                await RequestNextBlockAsync(connection);
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
        lock (_lock)
        {
            // Initialize piece buffer if needed
            var pieceSize = GetPieceSize(piece.Index);
            if (_pieces[piece.Index] == null)
                _pieces[piece.Index] = new byte[pieceSize];

            // Copy block data into piece
            Buffer.BlockCopy(piece.Block, 0, _pieces[piece.Index], piece.Begin, piece.Block.Length);
        }

        _logger.LogDebug("Received: piece {Index}, offset {Offset}, {Bytes} bytes",
            piece.Index, piece.Begin, piece.Block.Length);

        // Check if piece is complete
        var nextOffset = piece.Begin + piece.Block.Length;
        var pieceComplete = nextOffset >= GetPieceSize(piece.Index);

        if (pieceComplete)
        {
            if (VerifyPiece(piece.Index))
            {
                _completedPieces[piece.Index] = true;
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
                _currentBlockOffset = 0;
            }
        }
        else
        {
            _currentBlockOffset = nextOffset;
        }

        // Request next block if not complete
        if (!IsComplete)
        {
            await RequestNextBlockAsync(connection);
        }
        else
        {
            _logger.LogInformation("🎉 Download complete! File saved to: {Path}", _outputPath);
        }
    }

    private async Task RequestNextBlockAsync(PeerConnection connection)
    {
        if (_currentPieceIndex >= TotalPieces)
        {
            _logger.LogInformation("All pieces requested");
            return;
        }

        var pieceSize = GetPieceSize(_currentPieceIndex);
        var remainingInPiece = pieceSize - _currentBlockOffset;
        var blockLength = Math.Min(BlockSize, remainingInPiece);

        await connection.SendRequestAsync(_currentPieceIndex, _currentBlockOffset, blockLength);
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
