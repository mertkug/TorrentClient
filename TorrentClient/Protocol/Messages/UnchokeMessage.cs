using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Unchoke (ID=1)
/// No payload. Format: [0,0,0,1] [0]
/// </summary>
public class UnchokeMessage : IPeerMessage
{
    public byte? MessageId => 1;

    public byte[] Serialize()
    {
        var buffer = new byte[5];
        BinaryPrimitives.WriteInt32BigEndian(buffer, 1); // length = 1
        buffer[4] = 1; // message ID
        return buffer;
    }
}