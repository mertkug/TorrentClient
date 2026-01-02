using System.Buffers.Binary;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Interested (ID=2): Tell peer you want to download from them
/// No payload. Format: [0,0,0,1] [2]
/// </summary>
public class InterestedMessage : IPeerMessage
{
    public byte? MessageId => 2;

    public byte[] Serialize()
    {
        var buffer = new byte[5];
        BinaryPrimitives.WriteInt32BigEndian(buffer, 1); // length = 1
        buffer[4] = 2; // message ID
        return buffer;
    }
}
