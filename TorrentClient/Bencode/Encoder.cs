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
        stringBase = stringBase ?? throw new ArgumentNullException(nameof(stringBase));
        // call EncodeToString, EncodeToString etc. based on which type coming from base
        return stringBase switch
        {
            BencodedString bencodedString => EncodeToString(bencodedString),
            BencodedInteger bencodedInteger => EncodeToInteger(bencodedInteger),
            BencodedList<IBencodedBase> bencodedList => EncodeToList(bencodedList),
            BencodedDictionary<BencodedString, IBencodedBase> bencodedDictionary => EncodeToDictionary(bencodedDictionary),
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

    private string EncodeToInteger(BencodedInteger bencodedInteger)
    {
        return $"i{bencodedInteger.Value}e";
    }

    private string EncodeToString(BencodedString bencodedString)
    {
        return $"{bencodedString.Value.Length}:{bencodedString.Value}";
    }
}