namespace TorrentClient.Types.Bencoded;

public class BencodedInteger: IBencodedDictionaryValue, IBencodedBase
{
    public long Value { get; set; }
    public BencodedInteger(long value)
    {
        Value = value;
    }
    public override string ToString()
    {
        return Value.ToString();
    }
}