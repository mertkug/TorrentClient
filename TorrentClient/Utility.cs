using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TorrentClient;

public static class Utility
{
    public static byte[] ReadStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    public static bool ContainsNonAscii(string str)
    {
        return str.Any(c => c > 127);
    }
    public static string CalculateInfoHashFromBytes(byte[] infoBytes)
    {
        var hash = SHA1.HashData(infoBytes);
        var hexData = Convert.ToHexString(hash);
        return hexData;
    }
    public static string GeneratePeerId()
    {
        var peerId = new StringBuilder("-AZ2060-");
        var random = new Random();
        for (var i = 0; i < 12; i++)
        {
            peerId.Append(random.Next(0, 9));
        }
        return peerId.ToString();
    }
}
