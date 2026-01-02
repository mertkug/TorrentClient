namespace TorrentClient.Enums;

public enum TrackerEvent
{
    None,       // Regular interval announce
    Started,    // First request
    Stopped,    // Shutting down
    Completed   // Download finished
}