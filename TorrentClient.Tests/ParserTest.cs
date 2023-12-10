using System.Text;
using TorrentClient.Bencode;

namespace TorrentClient.Tests;

public class Tests
{
    private readonly Parser _parser = new();

    private static IEnumerable<object[]> TestCases()
    {
        yield return new object[]
            { "d3:cow3:moo4:spam4:eggse", new Dictionary<string, string> { { "cow", "moo" }, { "spam", "eggs" } } };
        yield return new object[]
            { "d3:cowi12e4:spam4:eggse", new Dictionary<string, object> { { "cow", 12 }, { "spam", "eggs" } } };
        yield return new object[]
            { "d4:spaml1:a1:bee", new Dictionary<string, List<string>> { { "spam", new List<string> { "a", "b" } } } };
        yield return new object[]
        {
            "d9:publisher3:bob17:publisher-webpage15:www.example.com18:publisher.location4:homee",
            new Dictionary<string, string>
                { { "publisher", "bob" }, { "publisher-webpage", "www.example.com" }, { "publisher.location", "home" } }
        };
        yield return new object[]
        {
            "d8:announce35:udp://tracker.openbittorrent.com:8013:creation datei1327049827e", new Dictionary<string, object>
            {
                { "announce", "udp://tracker.openbittorrent.com:80" },
                { "creation date", 1327049827 }
            }
        };
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public Task ParseInput_ReturnsExpectedResult(string input, object expected)
    {
        // Act & Assert
        Assert.That(_parser.Decode<object>(CreateStream(input)), Is.EqualTo(expected));
        return Task.CompletedTask;
    }

    private static Stream CreateStream(string input)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(input));
    }
}
