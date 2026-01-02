public class TrackerResponse
{
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? WarningMessage { get; set; }
    public int Interval { get; set; }          // Seconds until next announce
    public int? MinInterval { get; set; }       // Optional minimum interval
    public int Complete { get; set; }           // Number of seeders
    public int Incomplete { get; set; }         // Number of leechers
    public byte[] PeersData { get; set; }       // Raw compact peer data
    public string? TrackerId { get; set; }      // For subsequent requests
}