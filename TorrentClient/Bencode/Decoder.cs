using System.Collections.Specialized;
using System.Text;
using TorrentClient.Models;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Bencode;

public class Decoder : IDecoder
{
    
    public IBencodedBase Decode(Stream stream) => Decode(Utility.ReadStream(stream));
    
    public IBencodedBase DecodeFromBytes(byte[] input) => Decode(input);

    public IBencodedBase DecodeString(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        var colonIndex = encoded[currentIndex..].IndexOf((byte)':');
        if (colonIndex == -1)
            throw new InvalidOperationException("Invalid encoded value");
        colonIndex += currentIndex; // Relative to start

        var strLengthStr = Encoding.UTF8.GetString(encoded.Slice(currentIndex, colonIndex - currentIndex));
        if (!int.TryParse(strLengthStr, out var strLength))
            throw new InvalidOperationException("Invalid length value");

        var buffer = new byte[strLength];

        for (var i = 0; i < strLength; i++)
        {
            buffer[i] = encoded[colonIndex + 1 + i];
        }

        var strValue = Encoding.UTF8.GetString(buffer);

        // Check if the string contains non-ASCII characters
        if (Utility.ContainsNonAscii(strValue))
        {
            Console.WriteLine(strValue);
            currentIndex = colonIndex + 1 + strLength;

            return new BencodedByteStream(buffer);
        }

        currentIndex = colonIndex + 1 + strLength;

        return new BencodedString(strValue);
    }
    public BencodedInteger DecodeInteger(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'i')
            throw new InvalidOperationException("Invalid encoded integer");

        encoded = encoded[(currentIndex + 1)..];
        currentIndex++;

        var exclusiveEndIndex = encoded.IndexOf((byte)'e');
        if (exclusiveEndIndex == -1)
            throw new InvalidOperationException("Invalid encoded integer");
        currentIndex += exclusiveEndIndex + 1;
        
        return new BencodedInteger(long.Parse(Encoding.UTF8.GetString(encoded[..exclusiveEndIndex])));
    }
    
    public BencodedList<IBencodedBase> DecodeList(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'l')
            throw new InvalidOperationException("Invalid encoded list");

        var listContent = encoded[(currentIndex + 1)..];
        var list = new BencodedList<IBencodedBase>();
        var innerIndex = 0;

        while (innerIndex < listContent.Length && listContent[innerIndex] != (byte)'e')
        {
            var value = GetNextElement(listContent, ref innerIndex);
            list.Value.Add(value);
        }

        // Move the outer index past the 'e' character
        currentIndex += innerIndex + 1;

        return list;
    }

    public BencodedDictionary<BencodedString, IBencodedBase> DecodeDictionary(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'d')
            throw new InvalidOperationException("Invalid encoded dictionary");

        var dictContent = encoded[(currentIndex + 1)..];
        var dictionary = new SortedDictionary<BencodedString, IBencodedBase>();
        var innerIndex = 0;

        while (innerIndex < dictContent.Length && dictContent[innerIndex] != (byte)'e')
        {
            // Decode key
            var key = (BencodedString)DecodeString(dictContent, ref innerIndex);

            // Decode value
            var value = GetNextElement(dictContent, ref innerIndex);

            // Add key-value pair to dictionary
            dictionary[key] = value;
        }

        // Move the outer index past the 'e' character
        currentIndex += innerIndex + 1;

        return new BencodedDictionary<BencodedString, IBencodedBase>(dictionary);
    }
    

    public IBencodedBase GetNextElement(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        return encoded[currentIndex] switch
        {
            (byte)'i' => DecodeInteger(encoded, ref currentIndex),
            (byte)'l' => DecodeList(encoded, ref currentIndex),
            (byte)'d' => DecodeDictionary(encoded, ref currentIndex),
            _ => DecodeString(encoded, ref currentIndex)
        };
    }

    public IBencodedBase Decode(ReadOnlySpan<byte> encoded)
    {
        var currentIndex = 0;

        return encoded[0] switch
        {
            (byte)'i' => DecodeInteger(encoded, ref currentIndex),
            (byte)'l' => DecodeList(encoded, ref currentIndex),
            (byte)'d' => DecodeDictionary(encoded, ref currentIndex),
            _ => DecodeString(encoded, ref currentIndex)
        };
    }
}