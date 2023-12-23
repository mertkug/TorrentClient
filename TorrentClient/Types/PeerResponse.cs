namespace TorrentClient.Types;

public class PeerResponse
{
    public long Interval { get; }
    public long Complete { get; }
    public long Incomplete { get; }
    public List<Peer> Peers { get; }
}