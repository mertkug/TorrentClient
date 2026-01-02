using System.Buffers.Binary;
using System.Net.Sockets;

namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Parses incoming peer wire protocol messages from network stream
/// </summary>
public static class MessageParser
{
    /// <summary>
    /// Read and parse the next message from stream
    /// </summary>
    public static async Task<IPeerMessage> ReadMessageAsync(NetworkStream stream, CancellationToken ct = default)
    {
        // Step 1: Read 4-byte length prefix (big-endian)
        var lengthBuffer = new byte[4];
        await ReadExactAsync(stream, lengthBuffer, ct);
        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
        
        // Step 2: Handle keep-alive (length = 0)
        if (length == 0)
        {
            return new KeepAliveMessage();
        }
        
        // Step 3: Read the rest of the message
        var payload = new byte[length];
        await ReadExactAsync(stream, payload, ct);
        
        var messageId = payload[0];
        var messagePayload = payload.AsSpan(1); // everything after message ID
        
        // Step 4: Parse based on message ID
        return messageId switch
        {
            0 => new ChokeMessage(),
            1 => ParseUnchoke(),
            2 => new InterestedMessage(),
            3 => ParseNotInterested(),
            4 => ParseHave(messagePayload),
            5 => ParseBitfield(messagePayload),
            6 => ParseRequest(messagePayload),
            7 => ParsePiece(messagePayload),
            8 => ParseCancel(messagePayload),
            _ => throw new NotSupportedException($"Unknown message ID: {messageId}")
        };
    }
    
    /// <summary>
    /// Read exactly 'count' bytes from stream (network may fragment)
    /// </summary>
    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            if (read == 0)
                throw new EndOfStreamException("Connection closed by peer");
            offset += read;
        }
    }
    
    private static IPeerMessage ParsePiece(ReadOnlySpan<byte> payload)
    {
        var index = BinaryPrimitives.ReadInt32BigEndian(payload[..4]);
        var begin = BinaryPrimitives.ReadInt32BigEndian(payload[4..8]);
        var block = payload[8..].ToArray();
        return new PieceMessage(index, begin, block);
    }
    
    
    private static UnchokeMessage ParseUnchoke()
    {
        return new UnchokeMessage();
    }
    
    private static NotInterestedMessage ParseNotInterested()
    {
        return new NotInterestedMessage();
    }
    
    private static IPeerMessage ParseHave(ReadOnlySpan<byte> payload)
    {
        return new HaveMessage(BinaryPrimitives.ReadInt32BigEndian(payload));
    }
    
    private static IPeerMessage ParseBitfield(ReadOnlySpan<byte> payload)
    {
        return new BitfieldMessage(payload.ToArray());
    }
    
    private static IPeerMessage ParseRequest(ReadOnlySpan<byte> payload)
    {
        return new RequestMessage(
            BinaryPrimitives.ReadInt32BigEndian(payload[..4]),
            BinaryPrimitives.ReadInt32BigEndian(payload[4..8]),
            BinaryPrimitives.ReadInt32BigEndian(payload[8..12])
        );
    }
    
    private static IPeerMessage ParseCancel(ReadOnlySpan<byte> payload)
    {
        return new CancelMessage(
            BinaryPrimitives.ReadInt32BigEndian(payload[..4]),
            BinaryPrimitives.ReadInt32BigEndian(payload[4..8]),
            BinaryPrimitives.ReadInt32BigEndian(payload[8..12])
        );
    }
}
