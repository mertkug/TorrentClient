using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace TorrentClient.Tcp;

public record HandshakeRequest(IPAddress PeerIp, int PeerPort, string PeerId, string InfoHash);
public record HandshakeResult(bool Success, object? Response, string? Error);

public class PeerListener : BackgroundService
{
    private readonly ILogger<PeerListener> _logger;
    private readonly Channel<(HandshakeRequest Request, TaskCompletionSource<HandshakeResult> Tcs)> _handshakeQueue;

    public PeerListener(ILogger<PeerListener> logger)
    {
        _logger = logger;
        _handshakeQueue = Channel.CreateUnbounded<(HandshakeRequest, TaskCompletionSource<HandshakeResult>)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeerListener started");

        await foreach (var (request, tcs) in _handshakeQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var response = await PerformHandShakeAsync(request, stoppingToken);
                tcs.SetResult(new HandshakeResult(true, ParseResponse(response), null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handshake failed for peer {PeerIp}:{PeerPort}", request.PeerIp, request.PeerPort);
                tcs.SetResult(new HandshakeResult(false, null, ex.Message));
            }
        }

        _logger.LogInformation("PeerListener stopped");
    }

    /// <summary>
    /// Queues a handshake request and returns the result asynchronously
    /// </summary>
    public async Task<HandshakeResult> QueueHandshakeAsync(IPAddress peerIp, int peerPort, string peerId, string infoHash)
    {
        var request = new HandshakeRequest(peerIp, peerPort, peerId, infoHash);
        var tcs = new TaskCompletionSource<HandshakeResult>();
        await _handshakeQueue.Writer.WriteAsync((request, tcs));
        return await tcs.Task;
    }

    private static async Task<byte[]> PerformHandShakeAsync(HandshakeRequest request, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(request.PeerIp, request.PeerPort, cancellationToken);
        var stream = client.GetStream();

        var handshake = new byte[68];
        handshake[0] = 19;
        var protocol = "BitTorrent protocol"u8.ToArray();
        Buffer.BlockCopy(protocol, 0, handshake, 1, protocol.Length);
        var reserved = new byte[8];
        Buffer.BlockCopy(reserved, 0, handshake, 20, reserved.Length);
        Buffer.BlockCopy(Convert.FromHexString(request.InfoHash), 0, handshake, 28, 20);
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(request.PeerId), 0, handshake, 48, 20);

        await stream.WriteAsync(handshake, cancellationToken);

        var response = new byte[68];
        var read = await stream.ReadAsync(response, cancellationToken);

        if (read != 68)
        {
            throw new InvalidOperationException($"Handshake failed: expected 68 bytes, got {read}");
        }

        return response;
    }

    /// <summary>
    /// Parse response from peer
    /// </summary>
    /// <param name="response">Response from peer</param>
    /// <returns>Anonymous type structured as: { protocol: string, infoHash: string, peerId: string }</returns>
    public static object ParseResponse(byte[] response)
    {
        var protocol = Encoding.UTF8.GetString(response[1..20]);
        var infoHash = Convert.ToHexString(response[28..48]);
        var peerId = Convert.ToHexString(response[48..]);
        return new { protocol, infoHash, peerId };
    }
}