using System.Security.Cryptography;
using System.Text;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Models;

public class Torrent
{
    private Lazy<string> _lazyHash = new(() => string.Empty);

    public string? Announce { get; set; }
    public TorrentInfo Info { get; set; }
    
    public List<Peer> Peers { get; set; } = new();

    public string InfoHash => _lazyHash.Value;
    
    public void SetInfo(TorrentInfo info)
    {
        Info = info;
    }
    
    public void SetInfoHash(string infoHash)
    {
        _lazyHash = new Lazy<string>(() => infoHash);
    }


}