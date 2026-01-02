namespace TorrentClient.Protocol.Messages;

/// <summary>
/// Piece (ID=7): Actual data from peer!
/// Payload: [4 bytes index] [4 bytes begin] [block data...]
/// </summary>
public class PieceMessage : IPeerMessage
{
    public byte? MessageId => 7;
    
    /// <summary>Piece index</summary>
    public int Index { get; }
    
    /// <summary>Byte offset within the piece</summary>
    public int Begin { get; }
    
    /// <summary>The actual block data</summary>
    public byte[] Block { get; }

    public PieceMessage(int index, int begin, byte[] block)
    {
        Index = index;
        Begin = begin;
        Block = block;
    }

    public byte[] Serialize()
    {
        // TODO: until seeding support
        throw new NotImplementedException("Implement when adding seeding support");
    }
}
