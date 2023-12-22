namespace TorrentClient.Types.Bencoded;

/// <summary>
/// Base interface for all bencoded types
/// </summary>
public interface IBencodedBase
{
    public bool Equals(object? obj);
    public int GetHashCode();
}