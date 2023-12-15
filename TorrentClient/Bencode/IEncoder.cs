using TorrentClient.Types.Bencoded;

namespace TorrentClient.Bencode;

public interface IEncoder
{
    public string Encode(IBencodedBase stringBase);
}