namespace TorrentClient.Types.Bencoded;

public class BencodedList<T> : IBencodedBase
{
    public List<T> Value { get; set; }
    public BencodedList(List<T> value)
    {
        Value = value;
    }
    public BencodedList()
    {
        Value = new List<T>();
    }
}