namespace TorrentClient.Types.Bencoded;

public class BencodedString: IBencodedBase, IEquatable<BencodedString>, IComparable<BencodedString>
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
        return obj.GetType() == GetType() && Equals((BencodedString)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public int CompareTo(BencodedString? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return ReferenceEquals(null, other) ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
    }
}