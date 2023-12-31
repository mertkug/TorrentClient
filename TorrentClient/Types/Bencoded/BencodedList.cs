namespace TorrentClient.Types.Bencoded;

public class BencodedList<T> : IBencodedBase, IEquatable<BencodedList<T>>
{
    public List<T> Value { get; }
    public BencodedList(List<T> value)
    {
        Value = value;
    }
    public BencodedList()
    {
        Value = new List<T>();
    }

    public bool Equals(BencodedList<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) ||
               other.Value.SequenceEqual(Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((BencodedList<T>)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}