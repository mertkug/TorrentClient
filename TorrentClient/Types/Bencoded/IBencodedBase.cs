namespace TorrentClient.Types.Bencoded;

public interface IBencodedBase
{
    public bool Equals(object? obj);
    public int GetHashCode();
}