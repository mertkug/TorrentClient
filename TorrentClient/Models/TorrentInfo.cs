namespace TorrentClient.Models;

public class TorrentInfo
{
    private Lazy<string[]> _lazyPiecesHash = new(Array.Empty<string>);
    public string? Name { get; set; }
    public long Length { get; set; }
    public long PieceLength { get; set; }
    public byte[] Pieces { get; set; }
    
    public string[] PiecesHash => _lazyPiecesHash.Value;
    
    public void SetPiecesHash(string[] piecesHash)
    {
        _lazyPiecesHash = new Lazy<string[]>(() => piecesHash);
    }
}