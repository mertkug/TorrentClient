using TorrentClient.Models;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Services;

public interface ITorrentService
{
    public Torrent ConvertToTorrent(IBencodedBase torrentDictionary, byte[] encodedBytes);
}