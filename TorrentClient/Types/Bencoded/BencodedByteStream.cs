namespace TorrentClient.Types.Bencoded;

public class BencodedByteStream : IEquatable<BencodedByteStream>, IComparable<BencodedByteStream>, IBencodedBase
{
    public byte[] Value { get; }
    public BencodedByteStream(byte[] value)
    {
        Value = value;
    }
    public override string ToString()
    {
        return Value.ToString();
    }
    public bool Equals(BencodedByteStream? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || Value.SequenceEqual(other.Value);
    }

    public int CompareTo(BencodedByteStream? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return ReferenceEquals(null, other) ? 1 :
            CompareByteArrays(Value, other.Value);
    }

    private static int CompareByteArrays(IReadOnlyCollection<byte> first, IReadOnlyList<byte> second)
    {
        if (first.Count != second.Count)
            return first.Count.CompareTo(second.Count);

        return first.Select((t, i) => t.CompareTo(second[i])).FirstOrDefault(comparison => comparison != 0);
    }
}