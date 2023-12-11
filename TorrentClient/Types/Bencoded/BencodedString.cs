namespace TorrentClient.Types.Bencoded;

public class BencodedString: IBencodedDictionaryValue, IBencodedBase, IEquatable<BencodedString>
{
    public string Value { get; }
    public BencodedString(string value)
    {
        Value = value;
    }

    public bool Equals(BencodedString? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((BencodedString)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}