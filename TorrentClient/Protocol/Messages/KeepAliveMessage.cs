namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Keep-alive: no message ID, no payload, just [0,0,0,0]
/// Sent to prevent connection timeout (~every 2 minutes)
/// </summary>
public class KeepAliveMessage : IPeerMessage
{
    public byte? MessageId => null;

    public byte[] Serialize()
    {
        // Length = 0, no ID, no payload
        return [0, 0, 0, 0];
    }
}
