using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace TorrentClient.Bencode;

public class Parser : IParser
{
    
    public T Decode<T>(Stream stream)
    {
        var bufferArray = Utility.ReadStream(stream);
        
        var memory = new ReadOnlyMemory<byte>(bufferArray);
        
        var span = memory.Span;
        
        var decodedValue = Decode<T>(span);
        
        return decodedValue;
    }

    public string DecodeString(ReadOnlySpan<byte> encoded, ref int currentIndex)
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

        currentIndex = colonIndex + 1 + strLength;
 
        return strValue;
    }

    public long DecodeInteger(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'i')
            throw new InvalidOperationException("Invalid encoded integer");

        encoded = encoded[(currentIndex + 1)..];
        currentIndex++;

        var exclusiveEndIndex = encoded.IndexOf((byte)'e');
        if (exclusiveEndIndex == -1)
            throw new InvalidOperationException("Invalid encoded integer");
        currentIndex += exclusiveEndIndex + 1;

        return long.Parse(Encoding.UTF8.GetString(encoded[..exclusiveEndIndex]));
    }

    public IEnumerable<T> DecodeList<T>(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'l')
            throw new InvalidOperationException("Invalid encoded list");

        var listContent = encoded[(currentIndex + 1)..];
        var elements = new List<T>();
        var innerIndex = 0;

        while (innerIndex < listContent.Length && listContent[innerIndex] != (byte)'e')
        {
            var element = GetNextElement<T>(listContent, ref innerIndex);

            // Add each element to the elements list
            elements.Add(element);
        }

        // Move the outer index past the 'e' character
        currentIndex += innerIndex + 1;

        return elements;
    }

    public Dictionary<string, T> DecodeDictionary<T>(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        if (encoded[currentIndex] != (byte)'d')
            throw new InvalidOperationException("Invalid encoded dictionary");

        var dictContent = encoded[(currentIndex + 1)..];
        var dictionary = new Dictionary<string, T>();
        var innerIndex = 0;

        while (innerIndex < dictContent.Length && dictContent[innerIndex] != (byte)'e')
        {
            // Decode key
            var key = DecodeString(dictContent, ref innerIndex);

            // Decode value
            var value = GetNextElement<T>(dictContent, ref innerIndex);

            // Add key-value pair to dictionary
            dictionary[key] = value;
        }

        // Move the outer index past the 'e' character
        currentIndex += innerIndex + 1;

        return dictionary;
    }
    

    public T GetNextElement<T>(ReadOnlySpan<byte> encoded, ref int currentIndex)
    {
        
        return encoded[currentIndex] switch
        {
            (byte)'i' => (T)(object)DecodeInteger(encoded, ref currentIndex),
            (byte)'l' => (T)(object)DecodeList<T>(encoded, ref currentIndex),
            (byte)'d' => (T)(object)DecodeDictionary<T>(encoded, ref currentIndex),
            _ => (T)(object)DecodeString(encoded, ref currentIndex)
        };
    }

    public T Decode<T>(ReadOnlySpan<byte> encoded)
    {
        var currentIndex = 0;

        return encoded[0] switch
        {
            (byte)'i' => (T)(object)DecodeInteger(encoded, ref currentIndex),
            (byte)'l' => (T)DecodeList<T>(encoded, ref currentIndex),
            (byte)'d' => (T)(object)DecodeDictionary<T>(encoded, ref currentIndex),
            _ => (T)(object)DecodeString(encoded, ref currentIndex)
        };
    }
}