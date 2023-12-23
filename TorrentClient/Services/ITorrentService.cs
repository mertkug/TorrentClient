using TorrentClient.Models;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Services;

public interface ITorrentService
{
    public Torrent ConvertToTorrent(IBencodedBase torrentDictionary);
    public Task<byte[]> GetPeers(Torrent torrent);
    public void SetPeers(Torrent torrent, byte[] peers);
}