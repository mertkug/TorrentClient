using System.Text;
using TorrentClient.Models;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Bencode;


public class Encoder
{
    public Encoder()
    {
        
    }
    
    public string Encode(IBencodedBase stringBase)
    {
        ArgumentNullException.ThrowIfNull(stringBase);
        // call EncodeToString, EncodeToString etc. based on which type coming from base
        return stringBase switch
        {
            BencodedString bencodedString => EncodeToString(bencodedString),
            BencodedInteger bencodedInteger => EncodeToInteger(bencodedInteger),
            BencodedList<IBencodedBase> bencodedList => EncodeToList(bencodedList),
            BencodedDictionary<BencodedString, IBencodedBase> bencodedDictionary => EncodeToDictionary(bencodedDictionary),
            BencodedByteStream bencodedByteStream => EncodeToString(new BencodedString(Encoding.UTF8.GetString(bencodedByteStream.Value))),
            _ => throw new InvalidOperationException("Invalid type")
        };
    }
    private string EncodeToDictionary(BencodedDictionary<BencodedString, IBencodedBase> bencodedDictionary)
    {
        return $"d{string.Join("", bencodedDictionary.Value.Select(x => $"{Encode(x.Key)}{Encode(x.Value)}"))}e";
    }

    private string EncodeToList(BencodedList<IBencodedBase> bencodedList)
    {
        return $"l{string.Join("", bencodedList.Value.Select(Encode))}e";
    }

    private static string EncodeToInteger(BencodedInteger bencodedInteger)
    {
        return $"i{bencodedInteger.Value}e";
    }

    private static string EncodeToString(BencodedString bencodedString)
    {
        return $"{bencodedString.Value.Length}:{bencodedString.Value}";
    }
    
    public byte[] EncodeToBytes(IBencodedBase stringBase)
    {
        ArgumentNullException.ThrowIfNull(stringBase);

        // call EncodeToString, EncodeToString etc. based on which type is coming from the base
        return stringBase switch
        {
            BencodedString bencodedString => EncodeStringToBytes(bencodedString),
            BencodedInteger bencodedInteger => EncodeIntegerToBytes(bencodedInteger),
            BencodedList<IBencodedBase> bencodedList => EncodeListToBytes(bencodedList),
            BencodedDictionary<BencodedString, IBencodedBase> bencodedDictionary => EncodeDictionaryToBytes(bencodedDictionary),
            BencodedByteStream bencodedByteStream => EncodeByteStreamToBytes(bencodedByteStream),
            _ => throw new InvalidOperationException("Invalid type")
        };
    }

    private byte[] EncodeDictionaryToBytes(BencodedDictionary<BencodedString, IBencodedBase> bencodedDictionary)
    {
        var result = new List<byte> { (byte)'d' };

        foreach (var pair in bencodedDictionary.Value)
        {
            result.AddRange(EncodeToBytes(pair.Key));
            result.AddRange(EncodeToBytes(pair.Value));
        }

        result.Add((byte)'e');
        
        Console.WriteLine(result.Count);
        return result.ToArray();
    }
    
    private byte[] EncodeByteStreamToBytes(BencodedByteStream bencodedByteStream)
    {
        var lengthBytes = Encoding.UTF8.GetBytes($"{bencodedByteStream.Value.Length}:");
        var resultArr = new byte[lengthBytes.Length + bencodedByteStream.Value.Length];

        // Copy the length bytes to resultArr
        Array.Copy(lengthBytes, resultArr, lengthBytes.Length);

        // Copy the value bytes to resultArr
        Array.Copy(bencodedByteStream.Value, 0, resultArr, lengthBytes.Length, bencodedByteStream.Value.Length);

        return resultArr;
    }

    private byte[] EncodeListToBytes(BencodedList<IBencodedBase> bencodedList)
    {
        var result = new List<byte> { (byte)'l' };

        foreach (var item in bencodedList.Value)
        {
            result.AddRange(EncodeToBytes(item));
        }

        result.Add((byte)'e');
        
        return result.ToArray();
    }

    private static byte[] EncodeIntegerToBytes(BencodedInteger bencodedInteger)
    {
        return Encoding.UTF8.GetBytes($"i{bencodedInteger.Value}e");
    }

    private static byte[] EncodeStringToBytes(BencodedString bencodedString)
    {
        var encodedString = $"{bencodedString.Value.Length}:{bencodedString.Value}";
        return Encoding.UTF8.GetBytes(encodedString);
    }
}