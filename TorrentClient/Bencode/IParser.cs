
using TorrentClient.Models;
using TorrentClient.Types;
using TorrentClient.Types.Bencoded;

namespace TorrentClient.Bencode;

public interface IParser
{
    public Torrent Decode(Stream stream);
    public BencodedString DecodeString(ReadOnlySpan<byte> encoded, ref int currentIndex);

    public BencodedInteger DecodeInteger(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public BencodedList<IBencodedBase> DecodeList(ReadOnlySpan<byte> encoded, ref int currentIndex);

    public BencodedDictionary<BencodedString, IBencodedBase> DecodeDictionary(ReadOnlySpan<byte> encoded,
        ref int currentIndex);


    public IBencodedBase GetNextElement(ReadOnlySpan<byte> encoded, ref int currentIndex);
    public IBencodedBase Decode(ReadOnlySpan<byte> encoded);
}