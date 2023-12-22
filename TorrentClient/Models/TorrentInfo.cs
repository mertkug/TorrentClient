namespace TorrentClient.Models;

public class TorrentInfo
{
    public string? Name { get; set; }
    public long Length { get; set; }
    public long PieceLength { get; set; }
    public byte[] Pieces { get; set; }
}