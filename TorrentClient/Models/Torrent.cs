namespace TorrentClient.Models;

public class Torrent
{
    public string? Announce { get; set; }
    public TorrentInfo Info { get; set; }
}