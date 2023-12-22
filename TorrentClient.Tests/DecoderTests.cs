using System.Collections.Specialized;
using System.Text;
using TorrentClient.Bencode;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;
using Decoder = TorrentClient.Bencode.Decoder;

namespace TorrentClient.Tests;

public class Tests
{
    private readonly Decoder _decoder = new();

    private static IEnumerable<object[]> TestCases()
    {
        yield return new object[]
        {
            "d3:cow3:moo4:spam4:eggse",
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("cow"), new BencodedString("moo") },
                { new BencodedString("spam"), new BencodedString("eggs") }
            })
        };
        yield return new object[]
        {
            "d3:cowi12e4:spam4:eggse",
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("cow"), new BencodedInteger(12) },
                { new BencodedString("spam"), new BencodedString("eggs") }
            })
        };
        yield return new object[]
        {
            "d4:spaml1:a1:bee",
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("spam"), EntityCreator.CreateBencodedList(
                    new List<IBencodedBase>
                    {
                        new BencodedString("a"), new BencodedString("b") 
                    }) 
                }
            })
        };
        yield return new object[]
        {
            "d9:publisher3:bob17:publisher-webpage15:www.example.com18:publisher.location4:homee",
            EntityCreator.CreateBencodedDictionary(new SortedDictionary<BencodedString, IBencodedBase>
            {
                { new BencodedString("publisher"), new BencodedString("bob") },
                { new BencodedString("publisher-webpage"), new BencodedString("www.example.com") },
                { new BencodedString("publisher.location"), new BencodedString("home") }
            })
        };
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public Task ParseInput_ReturnsExpectedResult(string input, object expected)
    {
        // Act & Assert
        Assert.That(_decoder.Decode(CreateStream(input)), Is.EqualTo(expected));
        return Task.CompletedTask;
    }

    private static Stream CreateStream(string input)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(input));
    }
}
