namespace TorrentClient.Types.Bencoded;

public class BencodedDictionary<TY, T>: IBencodedBase where TY : notnull where T: IBencodedBase
{
    public Dictionary<TY, T> Value { get; set; }
    public BencodedDictionary(Dictionary<TY, T> value)
    {
        Value = value;
    }
    public T? this[TY key] => Value.TryGetValue(key, out var item) ? item : default;
}