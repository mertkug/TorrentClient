using System.Net;
using System.Threading.Channels;

namespace TorrentClient.Tcp;

public record HandshakeRequest(IPAddress PeerIp, int PeerPort, string PeerId, string InfoHash);
public record HandshakeResult(bool Success, object? Response, string? Error);

public class PeerListener : BackgroundService
{
    private readonly ILogger<PeerListener> _logger;
    private readonly ILogger<PeerConnection> _peerLogger;
    private readonly Channel<(HandshakeRequest Request, TaskCompletionSource<HandshakeResult> Tcs)> _handshakeQueue;

    public PeerListener(ILogger<PeerListener> logger, ILogger<PeerConnection> peerLogger)
    {
        _logger = logger;
        _peerLogger = peerLogger;
        _handshakeQueue = Channel.CreateUnbounded<(HandshakeRequest, TaskCompletionSource<HandshakeResult>)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeerListener started");

        await foreach (var (request, tcs) in _handshakeQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var connection = await PeerConnection.ConnectAsync(
                    request.PeerIp, 
                    request.PeerPort, 
                    request.PeerId, 
                    request.InfoHash,
                    _peerLogger,
                    stoppingToken);
                
                tcs.SetResult(new HandshakeResult(true, connection, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection failed for peer {PeerIp}:{PeerPort}", request.PeerIp, request.PeerPort);
                tcs.SetResult(new HandshakeResult(false, null, ex.Message));
            }
        }

        _logger.LogInformation("PeerListener stopped");
    }

    public async Task<HandshakeResult> QueueHandshakeAsync(IPAddress peerIp, int peerPort, string peerId, string infoHash)
    {
        var request = new HandshakeRequest(peerIp, peerPort, peerId, infoHash);
        var tcs = new TaskCompletionSource<HandshakeResult>();
        await _handshakeQueue.Writer.WriteAsync((request, tcs));
        return await tcs.Task;
    }
}