using System.Buffers.Binary;
using System.Text.Json;

namespace TorrentClient.Protocol.Messages;

public class HaveMessage : IPeerMessage
{
    public int PieceIndex { get; }
    
    public HaveMessage(int pieceIndex)
    {
        PieceIndex = pieceIndex;
    }
    
    public byte? MessageId => 4;
    public byte[] Serialize()
    {
        var buffer = new byte[9];  // 4 (length prefix) + 1 (ID) + 4 (piece index)
        BinaryPrimitives.WriteInt32BigEndian(buffer, 5); // length = 5
        buffer[4] = 4; // message ID = 4
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(5), PieceIndex);
        return buffer;
    }
}
