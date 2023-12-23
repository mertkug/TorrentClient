using System.Buffers.Binary;
using System.Net;
using System.Web;
using TorrentClient.Bencode;
using TorrentClient.Models;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;
using Encoder = TorrentClient.Bencode.Encoder;

namespace TorrentClient.Services;

public class TorrentService : ITorrentService
{
    private readonly Encoder _encoder;
    private readonly IDecoder _decoder;

    public TorrentService(Encoder encoder, IDecoder decoder)
    {
        _encoder = encoder;
        _decoder = decoder;
    }

    public Torrent ConvertToTorrent(IBencodedBase torrentDictionary)
    {
        var torrent = new Torrent();
        if (torrentDictionary is BencodedDictionary<BencodedString, IBencodedBase> dictionary)
        {
            if (dictionary[new BencodedString("info")] is BencodedDictionary<BencodedString, IBencodedBase> infoValue)
            {
                var info = ConvertToTorrentInfo(infoValue);
                torrent.SetInfo(info);
                torrent.SetInfoHash(Utility.CalculateInfoHashFromBytes(_encoder.EncodeToBytes(infoValue)));
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

        for (var i = 0; i < info.Pieces.Length; i += 20)
        {
            var pieceSpan = info.Pieces.AsSpan(i, 20);
            var pieceHashArray = info.PiecesHash.Append(Convert.ToHexString(pieceSpan)).ToArray();
            info.SetPiecesHash(pieceHashArray);
        }
        return info;
    }
    
    private static async Task<byte[]> DiscoverPeers(Torrent torrent)
    {
        var client = new HttpClient();

        var uriBuilder = new UriBuilder(torrent.Announce);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        var peerId = Utility.GeneratePeerId();
        Client.PeerId = peerId;
        query["peer_id"] = peerId;
        query["port"] = "6881";
        query["uploaded"] = "0";
        query["downloaded"] = "0";
        query["left"] = torrent.Info.Length.ToString();
        query["compact"] = "1";
        uriBuilder.Query = query.ToString();
        var infoHash = HttpUtility.UrlEncode(Convert.FromHexString(torrent.InfoHash));
        var announceUrl = uriBuilder + $"&info_hash={infoHash}";
        
        try
        {
            var response = await client.GetAsync(announceUrl);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            using var reader = new BinaryReader(contentStream);
            return reader.ReadBytes((int)contentStream.Length);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
    
    public async Task<byte[]> GetPeers(Torrent torrent)
    {
        var peers = await DiscoverPeers(torrent);
        var peerDict = (BencodedDictionary<BencodedString, IBencodedBase>)_decoder.DecodeFromBytes(peers);
        var peerStream = (BencodedByteStream)peerDict.Value[new BencodedString("peers")];
        return peerStream.Value;
    }
    
    public void SetPeers(Torrent torrent, byte[] peers)
    {
        var peersList = new List<Peer>();
        for (var i = 0; i < peers.Length; i += 6)
        {
            var peerSpan = peers.AsSpan(i, 6);
            
            var peer = new Peer
            {
                IpAddress = new IPAddress(peerSpan[..4]),
                Port = BinaryPrimitives.ReadUInt16BigEndian(peerSpan[4..])
            };
            peersList.Add(peer);
        }
        torrent.Peers = peersList;
    }
}