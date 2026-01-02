using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

public class NotInterestedMessage: IPeerMessage
{
    public byte? MessageId => 3;

    public byte[] Serialize()
    {
        var buffer = new byte[5];
        BinaryPrimitives.WriteInt32BigEndian(buffer, 1); // length = 1
        buffer[4] = 3; // message ID
        return buffer;
    }
}