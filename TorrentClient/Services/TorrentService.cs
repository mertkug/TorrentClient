using System.Security.Cryptography;
using System.Text;
using TorrentClient.Bencode;
using TorrentClient.Models;
using TorrentClient.Types.Bencoded;
using Encoder = TorrentClient.Bencode.Encoder;

namespace TorrentClient.Services;

public class TorrentService : ITorrentService
{
    private readonly Encoder _encoder;

    public TorrentService(Encoder encoder)
    {
        _encoder = encoder;
    }

    public Torrent ConvertToTorrent(IBencodedBase torrentDictionary, byte[] encodedBytes)
    {
        var torrent = new Torrent();
        if (torrentDictionary is BencodedDictionary<BencodedString, IBencodedBase> dictionary)
        {
            if (dictionary[new BencodedString("info")] is BencodedDictionary<BencodedString, IBencodedBase> infoValue)
            {
                var info = ConvertToTorrentInfo(infoValue);
                torrent.SetInfo(info);
                torrent.SetInfoHash(CalculateInfoHashFromBytes(_encoder.EncodeToBytes(infoValue)));
            }
            else
            {
                Console.WriteLine("The 'info' key was not found or the value is not of type BencodedDictionary.");
            }
            
            if (dictionary[new BencodedString("announce")] is BencodedString announceValue)
            {
                torrent.Announce = announceValue.Value;
            }
            else
            {
                Console.WriteLine("The 'announce' key was not found or the value is not of type BencodedString.");
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
        var pieces = (BencodedByteStream) dictionary.Value[new BencodedString("pieces")];
        info.Pieces = pieces.Value;
        var length = (BencodedInteger) dictionary.Value[new BencodedString("length")];
        info.Length = length.Value;
        var name = (BencodedString) dictionary.Value[new BencodedString("name")];
        info.Name = name.Value;
        
        return info;
    }
    public static string CalculateInfoHashFromBytes(byte[] infoBytes)
    {
        var hash = SHA1.HashData(infoBytes);
        var hexData = Convert.ToHexString(hash);
        Console.WriteLine(hexData);
        return hexData;
    }
}