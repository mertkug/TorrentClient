
namespace TorrentClient.Bencode;

public interface IParser
{
    public T Decode<T>(Stream stream);
    public string DecodeString(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public long DecodeInteger(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public IEnumerable<T> DecodeList<T>(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public Dictionary<string, T> DecodeDictionary<T>(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public T GetNextElement<T>(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public T Decode<T>(ReadOnlySpan<byte> encoded);
}