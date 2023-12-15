namespace TorrentClient.Types.Bencoded;

public class BencodedInteger: IBencodedBase, IEquatable<BencodedInteger>
{
    public long Value { get; }
    public BencodedInteger(long value)
    {
        Value = value;
    }
    public override string ToString()
    {
        return Value.ToString();
    }

    public bool Equals(BencodedInteger? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((BencodedInteger)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}