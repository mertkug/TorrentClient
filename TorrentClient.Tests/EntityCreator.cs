using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Tests;

public static class EntityCreator
{
    public static BencodedDictionary<BencodedString, IBencodedBase> CreateBencodedDictionary(OrderedDictionary<BencodedString, IBencodedBase> items)
    {
        return new BencodedDictionary<BencodedString, IBencodedBase>(items);
    }
    public static BencodedList<IBencodedBase> CreateBencodedList(List<IBencodedBase> items)
    {
        return new BencodedList<IBencodedBase>(items);
    }
}