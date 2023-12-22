using System.Security.Cryptography;
using System.Text;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Models;

public class Torrent
{
    private Lazy<string> _someHash = new(() => string.Empty);

    public string? Announce { get; set; }
    public TorrentInfo Info { get; set; }

    public string InfoHash => _someHash.Value;

    public void SetInfo(TorrentInfo info)
    {
        Info = info;
    }
    
    public void SetInfoHash(string infoHash)
    {
        _someHash = new Lazy<string>(() => infoHash);
    }
    
    private static string CalculateInfoHash(byte[] encodedByte)
    {
        var hash = SHA1.HashData(encodedByte);
        var hexData = Convert.ToHexString(hash);
        return hexData;
    }

}