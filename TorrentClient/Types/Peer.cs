using System.Net;

namespace TorrentClient.Types;

public class Peer
{
    public IPAddress IpAddress { get; set; }
    public int Port { get; set; }
}