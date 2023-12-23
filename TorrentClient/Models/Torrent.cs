using System.Security.Cryptography;
using System.Text;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Models;

public class Torrent
{

    public string? Announce { get; set; }
    public TorrentInfo Info { get; set; }
    
    public List<Peer> Peers { get; set; } = new();

    public string InfoHash { get; set; }

}