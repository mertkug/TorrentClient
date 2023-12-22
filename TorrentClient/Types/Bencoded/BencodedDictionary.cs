namespace TorrentClient.Types.Bencoded;

public class BencodedDictionary<TY, T> : IBencodedBase, IEquatable<BencodedDictionary<TY, T>> where TY : notnull where T : IBencodedBase
{
    public SortedDictionary<TY, T> Value { get; }

    public BencodedDictionary()
    {
        Value = new SortedDictionary<TY, T>();
    }

    public BencodedDictionary(SortedDictionary<TY, T> value)
    {
        Value = value;
    }

    public T? this[TY key] => Value.TryGetValue(key, out var item) ? item : default;

    public bool Equals(BencodedDictionary<TY, T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        
        if (Value.Count != other.Value.Count)
            return false;

        foreach (var pair in Value)
        {
            if (!other.Value.TryGetValue(pair.Key, out var otherValue) || !pair.Value.Equals(otherValue))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((BencodedDictionary<TY, T>)obj);
    }

    public override int GetHashCode()
    {
        // Combine hash codes of key-value pairs
        var hashCode = 17;

        foreach (var pair in Value)
        {
            hashCode = hashCode * 31 + pair.Key.GetHashCode();
            hashCode = hashCode * 31 + pair.Value.GetHashCode();
        }

        return hashCode;
    }
}