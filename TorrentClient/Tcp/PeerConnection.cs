using System.Net;
using System.Net.Sockets;
using System.Text;
using TorrentClient.Protocol.Messages;

namespace TorrentClient.Tcp;

/// <summary>
/// Manages a single peer connection - handles handshake, message loop, and state
/// </summary>
public class PeerConnection : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly ILogger<PeerConnection> _logger;
    private readonly string _infoHash;
    private readonly string _peerId;
    
    // Peer state
    public bool AmChoking { get; private set; } = true;      // We are choking peer
    public bool AmInterested { get; private set; } = false;  // We are interested in peer
    public bool PeerChoking { get; private set; } = true;    // Peer is choking us
    public bool PeerInterested { get; private set; } = false; // Peer interested in us
    public BitfieldMessage? PeerBitfield { get; private set; }
    
    public IPAddress PeerIp { get; }
    public int PeerPort { get; }
    public bool IsConnected => _client.Connected;

    private PeerConnection(TcpClient client, NetworkStream stream, 
        ILogger<PeerConnection> logger, string infoHash, string peerId,
        IPAddress peerIp, int peerPort)
    {
        _client = client;
        _stream = stream;
        _logger = logger;
        _infoHash = infoHash;
        _peerId = peerId;
        PeerIp = peerIp;
        PeerPort = peerPort;
    }

    /// <summary>
    /// Connect to a peer and perform handshake
    /// </summary>
    public static async Task<PeerConnection> ConnectAsync(
        IPAddress peerIp, int peerPort, string peerId, string infoHash,
        ILogger<PeerConnection> logger, CancellationToken ct = default)
    {
        var client = new TcpClient();
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        await client.ConnectAsync(peerIp, peerPort, cts.Token);
        var stream = client.GetStream();
        
        // Send handshake
        var handshake = BuildHandshake(infoHash, peerId);
        await stream.WriteAsync(handshake, cts.Token);
        
        // Receive handshake response
        var response = new byte[68];
        var read = await stream.ReadAsync(response, cts.Token);
        if (read != 68)
            throw new InvalidOperationException($"Handshake failed: expected 68 bytes, got {read}");
        
        // Validate info hash matches
        var responseInfoHash = Convert.ToHexString(response[28..48]);
        if (!responseInfoHash.Equals(infoHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Info hash mismatch!");
        
        logger.LogInformation("Handshake successful with {Ip}:{Port}", peerIp, peerPort);
        
        return new PeerConnection(client, stream, logger, infoHash, peerId, peerIp, peerPort);
    }

    /// <summary>
    /// Read and process messages in a loop
    /// </summary>
    public async Task RunMessageLoopAsync(
        Func<PeerConnection, IPeerMessage, Task> onMessageReceived,
        CancellationToken ct = default)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                _logger.LogInformation("Waiting for message...");
                var message = await MessageParser.ReadMessageAsync(_stream, ct);
                _logger.LogInformation("Received message: {Type}", message.GetType().Name);
                
                // Update state based on message
                switch (message)
                {
                    case ChokeMessage:
                        PeerChoking = true;
                        _logger.LogInformation("Peer choked us");
                        break;
                    case UnchokeMessage:
                        PeerChoking = false;
                        _logger.LogInformation("Peer unchoked us");
                        break;
                    case InterestedMessage:
                        PeerInterested = true;
                        break;
                    case NotInterestedMessage:
                        PeerInterested = false;
                        break;
                    case BitfieldMessage bitfield:
                        PeerBitfield = bitfield;
                        _logger.LogInformation("Received bitfield ({Bytes} bytes)", bitfield.Bitfield.Length);
                        break;
                    case HaveMessage have:
                        _logger.LogInformation("Peer has piece {Index}", have.PieceIndex);
                        break;
                    case PieceMessage piece:
                        _logger.LogInformation("Received piece data: idx={Index}, off={Offset}, len={Len}", 
                            piece.Index, piece.Begin, piece.Block.Length);
                        break;
                }
                
                // Notify caller
                await onMessageReceived(this, message);
            }
        }
        catch (EndOfStreamException)
        {
            _logger.LogInformation("Peer {Ip}:{Port} disconnected", PeerIp, PeerPort);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error in message loop for {Ip}:{Port}", PeerIp, PeerPort);
            throw;
        }
    }

    /// <summary>
    /// Send Interested message to peer
    /// </summary>
    public async Task SendInterestedAsync(CancellationToken ct = default)
    {
        if (AmInterested) return;
        
        await _stream.WriteAsync(new InterestedMessage().Serialize(), ct);
        AmInterested = true;
        _logger.LogInformation("Sent Interested message");
    }

    /// <summary>
    /// Send Request message for a specific block
    /// </summary>
    public async Task SendRequestAsync(int pieceIndex, int blockOffset, int length = RequestMessage.DefaultBlockSize, CancellationToken ct = default)
    {
        if (PeerChoking)
        {
            _logger.LogWarning("Cannot request - peer is choking us");
            return;
        }
        
        var request = new RequestMessage(pieceIndex, blockOffset, length);
        var bytes = request.Serialize();
        _logger.LogInformation("Sending request: piece {Index}, offset {Offset}, length {Length}", pieceIndex, blockOffset, length);
        _logger.LogInformation("Request bytes: {Hex}", Convert.ToHexString(bytes));
        await _stream.WriteAsync(bytes);
        await _stream.FlushAsync();
        _logger.LogInformation("Request sent and flushed");
    }

    private static byte[] BuildHandshake(string infoHash, string peerId)
    {
        var handshake = new byte[68];
        handshake[0] = 19;
        var protocol = "BitTorrent protocol"u8.ToArray();
        Buffer.BlockCopy(protocol, 0, handshake, 1, protocol.Length);
        var reserved = new byte[8];
        Buffer.BlockCopy(reserved, 0, handshake, 20, reserved.Length);
        Buffer.BlockCopy(Convert.FromHexString(infoHash), 0, handshake, 28, 20);
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(peerId), 0, handshake, 48, 20);
        return handshake;
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _client.Dispose();
    }
}
