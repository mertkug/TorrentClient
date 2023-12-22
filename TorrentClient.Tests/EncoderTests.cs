using TorrentClient.Bencode;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Tests;

public class EncoderTests
{
    private static IEnumerable<object[]> TestCases()
    {
        yield return new object[]
        {
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("cow"), new BencodedString("moo") },
                { new BencodedString("spam"), new BencodedString("eggs") }
            }),
            "d3:cow3:moo4:spam4:eggse"
        };
    }
    [Test]
    [TestCaseSource(nameof(TestCases))]
    public Task ParseInput_ReturnsExpectedResult(IBencodedBase input, object expected)
    {
        Encoder encoder = new();
        // Act & Assert
        Assert.That(encoder.Encode(input), Is.EqualTo(expected));
        return Task.CompletedTask;
    }
}
