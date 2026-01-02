using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Choke (ID=0)
/// No payload. Format: [0,0,0,1] [0]
/// </summary>
public class ChokeMessage : IPeerMessage
{
    public byte? MessageId => 0;

    public byte[] Serialize()
    {
        var buffer = new byte[5];
        BinaryPrimitives.WriteInt32BigEndian(buffer, 1); // length = 1
        buffer[4] = 0; // message ID
        return buffer;
    }
}
