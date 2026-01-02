using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Cancel (ID=8): Cancel a previously requested block
/// Payload: [4 bytes index] [4 bytes begin] [4 bytes length]
/// Same format as RequestMessage
/// </summary>
public class CancelMessage : IPeerMessage
{
    public byte? MessageId => 8;
    
    public int Index { get; }
    public int Begin { get; }
    public int Length { get; }

    public CancelMessage(int index, int begin, int length)
    {
        Index = index;
        Begin = begin;
        Length = length;
    }

    public byte[] Serialize()
    {
        var buffer = new byte[17];
        BinaryPrimitives.WriteInt32BigEndian(buffer, 13);
        buffer[4] = 8;
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(5, 4), Index);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(9, 4), Begin);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(13, 4), Length);
        return buffer;
    }
}
