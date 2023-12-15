using TorrentClient.Models;
using TorrentClient.Types.Bencoded;

namespace TorrentClient;

public static class Utility
{
    public static byte[] ReadStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    public static Torrent ConvertToTorrent(IBencodedBase torrentDictionary)
    {
        var torrent = new Torrent();
        if (torrentDictionary is BencodedDictionary<BencodedString, IBencodedBase> dictionary)
        {
            if (dictionary[new BencodedString("announce")] is BencodedString announceValue)
            {
                torrent.Announce = announceValue.Value;
            }
            else
            {
                Console.WriteLine("The 'announce' key was not found or the value is not of type BencodedString.");
            }

            if (dictionary[new BencodedString("info")] is BencodedDictionary<BencodedString, IBencodedBase> infoValue)
            {
                torrent.Info = ConvertToTorrentInfo(infoValue);
            }
            else
            {
                Console.WriteLine("The 'info' key was not found or the value is not of type BencodedDictionary.");
            }
        }
        else
        {
            Console.WriteLine("The provided dictionary is not of type BencodedDictionary.");
        }

        return torrent;
    }

    private static TorrentInfo ConvertToTorrentInfo(IBencodedBase torrentInfoDictionary)
    {
        var dictionary = (BencodedDictionary<BencodedString, IBencodedBase>) torrentInfoDictionary;
        var info = new TorrentInfo();
        var pieceLength = (BencodedInteger) dictionary.Value[new BencodedString("piece length")];
        info.PieceLength = pieceLength.Value;
        var pieces = (BencodedString) dictionary.Value[new BencodedString("pieces")];
        info.Pieces = pieces.Value;
        var length = (BencodedInteger) dictionary.Value[new BencodedString("length")];
        info.Length = length.Value;
        var name = (BencodedString) dictionary.Value[new BencodedString("name")];
        info.Name = name.Value;
        return info;
    }

}