namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Bitfield (ID=5): Sent after handshake to indicate which pieces the peer has
/// Each bit represents one piece (1 = have, 0 = don't have)
/// </summary>
public class BitfieldMessage : IPeerMessage
{
    public byte? MessageId => 5;
    
    /// <summary>
    /// Raw bitfield data - each bit represents a piece
    /// </summary>
    public byte[] Bitfield { get; }

    public BitfieldMessage(byte[] bitfield)
    {
        Bitfield = bitfield;
    }

    /// <summary>
    /// Check if peer has a specific piece
    /// </summary>
    /// <param name="pieceIndex">Zero-based piece index</param>
    /// <returns>True if peer has the piece</returns>
    public bool HasPiece(int pieceIndex)
    {
        var byteIndex = pieceIndex / 8;
        var bitIndex = 7 - (pieceIndex % 8); // MSB first
        
        if (byteIndex >= Bitfield.Length)
            return false;
            
        return (Bitfield[byteIndex] & (1 << bitIndex)) != 0;
    }

    public byte[] Serialize()
    {
        throw new NotImplementedException();
    }
}