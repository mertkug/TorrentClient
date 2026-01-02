using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Request (ID=6): Ask peer for a specific block of a piece
/// Payload: [4 bytes index] [4 bytes begin] [4 bytes length]
/// Typical block size is 16384 bytes (16KB)
/// </summary>
public class RequestMessage : IPeerMessage
{
    public byte? MessageId => 6;
    
    /// <summary>Piece index (zero-based)</summary>
    public int Index { get; }
    
    /// <summary>Byte offset within the piece</summary>
    public int Begin { get; }
    
    /// <summary>Block length (usually 16384)</summary>
    public int Length { get; }

    public const int DefaultBlockSize = 16384; // 16KB

    public RequestMessage(int index, int begin, int length = DefaultBlockSize)
    {
        Index = index;
        Begin = begin;
        Length = length;
    }

    public byte[] Serialize()
    {
        var buffer = new byte[17]; // 4 (length) + 1 (ID) + 12 (payload)
        BinaryPrimitives.WriteInt32BigEndian(buffer, 13); // Length = 1 (ID) + 12 (payload)
        buffer[4] = 6; // Message ID
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(5, 4), Index);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(9, 4), Begin);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(13, 4), Length);
        return buffer;
    }
}
