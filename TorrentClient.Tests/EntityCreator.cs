using TorrentClient.Types.Bencoded;

namespace TorrentClient.Tests;

public static class EntityCreator
{
    public static BencodedDictionary<BencodedString, IBencodedBase> CreateBencodedDictionary(Dictionary<BencodedString, IBencodedBase> items)
    {
        return new BencodedDictionary<BencodedString, IBencodedBase>(items);
    }
    public static BencodedString CreateBencodedString(string value)
    {
        return new BencodedString(value);
    }
    public static BencodedInteger CreateBencodedInteger(long value)
    {
        return new BencodedInteger(value);
    }
    public static BencodedList<IBencodedBase> CreateBencodedList(List<IBencodedBase> items)
    {
        return new BencodedList<IBencodedBase>(items);
    }
}