using System.Text;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;
using Encoder = TorrentClient.Bencode.Encoder;

namespace TorrentClient.Tests;

public class EncoderByteTests
{
    private static IEnumerable<object[]> TestCases()
    {
        yield return new object[]
        {
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("cow"), new BencodedString("moo") },
                { new BencodedString("spam"), new BencodedByteStream("eggs"u8.ToArray()) }
            }),
            "d3:cow3:moo4:spam4:eggse"u8.ToArray()
        };
    }
    [Test]
    [TestCaseSource(nameof(TestCases))]
    public Task ParseInput_ReturnsExpectedResult(IBencodedBase input, object expected)
    {
        Encoder encoder = new();
        // Act & Assert
        Assert.That(encoder.EncodeToBytes(input), Is.EqualTo(expected));
        return Task.CompletedTask;
    }
}