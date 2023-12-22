namespace TorrentClient;

public static class Utility
{
    public static byte[] ReadStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    public static bool ContainsNonAscii(string str)
    {
        return str.Any(c => c > 127);
    }
}