using System.Buffers.Binary;
using System.Net;
using System.Web;
using TorrentClient.Bencode;
using TorrentClient.Enums;
using TorrentClient.Models;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;
using Encoder = TorrentClient.Bencode.Encoder;

namespace TorrentClient.Services;

public class TorrentService
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
                torrent.Info = info;
                torrent.InfoHash = Utility.CalculateInfoHashFromBytes(_encoder.EncodeToBytes(infoValue));
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
        
        // Handle both single-file (length) and multi-file (files) torrents
        var lengthKey = new BencodedString("length");
        var filesKey = new BencodedString("files");
        
        if (dictionary.Value.ContainsKey(lengthKey))
        {
            // Single-file torrent
            var length = (BencodedInteger) dictionary.Value[lengthKey];
            info.Length = length.Value;
        }
        else if (dictionary.Value.ContainsKey(filesKey))
        {
            // Multi-file torrent - sum up all file lengths
            var files = (BencodedList<IBencodedBase>) dictionary.Value[filesKey];
            long totalLength = 0;
            foreach (var file in files.Value)
            {
                var fileDict = (BencodedDictionary<BencodedString, IBencodedBase>) file;
                var fileLength = (BencodedInteger) fileDict.Value[lengthKey];
                totalLength += fileLength.Value;
            }
            info.Length = totalLength;
        }
        
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
    
    private static async Task<byte[]> DiscoverPeers(Torrent torrent, TrackerEvent trackerEvent = TrackerEvent.None)
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
        query["numwant"] = "50";

        if (trackerEvent != TrackerEvent.None)
        {
            query["event"] = trackerEvent switch
            {
                TrackerEvent.Started => "started",
                TrackerEvent.Stopped => "stopped", 
                TrackerEvent.Completed => "completed",
                _ => null
            };
        }

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
    
    public async Task<TrackerResponse> GetPeers(Torrent torrent, TrackerEvent trackerEvent = TrackerEvent.None)
    {
        var responseBytes = await DiscoverPeers(torrent, trackerEvent);
        var peerDict = (BencodedDictionary<BencodedString, IBencodedBase>)_decoder.DecodeFromBytes(responseBytes);
        
        var response = new TrackerResponse();
        
        var failureKey = new BencodedString("failure reason");
        if (peerDict.Value.ContainsKey(failureKey))
        {
            response.Success = false;
            response.FailureReason = ((BencodedString)peerDict.Value[failureKey]).Value;
            return response;
        }
        
        response.Success = true;
        
        var intervalKey = new BencodedString("interval");
        if (peerDict.Value.ContainsKey(intervalKey))
        {
            response.Interval = (int)((BencodedInteger)peerDict.Value[intervalKey]).Value;
        }
        
        // Handle both compact and dictionary peer formats
        var peersKey = new BencodedString("peers");
        var peersValue = peerDict.Value[peersKey];
        
        if (peersValue is BencodedByteStream peerStream)
        {
            // Compact format: 6 bytes per peer (4 IP + 2 port)
            response.PeersData = peerStream.Value;
        }
        else if (peersValue is BencodedList<IBencodedBase> peerList)
        {
            // Dictionary format: list of {peer id, ip, port} dictionaries
            // Convert to compact format for consistency
            using var ms = new MemoryStream();
            foreach (var peerEntry in peerList.Value)
            {
                var peerDictEntry = (BencodedDictionary<BencodedString, IBencodedBase>)peerEntry;
                var ipStr = ((BencodedString)peerDictEntry.Value[new BencodedString("ip")]).Value;
                var port = (int)((BencodedInteger)peerDictEntry.Value[new BencodedString("port")]).Value;
                
                // Parse IP and write as 4 bytes
                var ipBytes = IPAddress.Parse(ipStr).GetAddressBytes();
                ms.Write(ipBytes, 0, 4);
                
                // Write port as 2 bytes big-endian
                ms.WriteByte((byte)(port >> 8));
                ms.WriteByte((byte)(port & 0xFF));
            }
            response.PeersData = ms.ToArray();
        }
        else
        {
            throw new InvalidOperationException($"Unknown peers format: {peersValue.GetType().Name}");
        }
        
        return response;
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