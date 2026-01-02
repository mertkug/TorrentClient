namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Base interface for all peer wire protocol messages
/// </summary>
public interface IPeerMessage
{
    /// <summary>
    /// Message ID (null for keep-alive)
    /// </summary>
    byte? MessageId { get; }
    
    /// <summary>
    /// Serialize message to bytes for sending over network
    /// </summary>
    byte[] Serialize();
}
