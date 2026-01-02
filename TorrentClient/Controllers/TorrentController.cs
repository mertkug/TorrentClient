using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using TorrentClient.Bencode;
using TorrentClient.Services;
using Encoder = TorrentClient.Bencode.Encoder;
using System.IO;
using System;
using TorrentClient.Models;
using TorrentClient.Tcp;
using TorrentClient.Types.Bencoded;
using TorrentClient.Enums;

namespace TorrentClient.Controllers;

[ApiController]
[Route("/api/")]
public class TorrentController : ControllerBase
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<TorrentController> _logger;
    private readonly ILogger<PeerConnection> _peerConnectionLogger;
    private readonly ILogger<TorrentDownloader> _downloaderLogger;
    private readonly IDecoder _decoder;
    private readonly TorrentService _torrentService;
    private readonly PeerListener _peerListener;

    public TorrentController(ILogger<TorrentController> logger, 
        ILogger<PeerConnection> peerConnectionLogger,
        ILogger<TorrentDownloader> downloaderLogger,
        IFileProvider fileProvider, 
        IDecoder decoder,
        TorrentService torrentService,
        PeerListener peerListener)
    {
        _fileProvider = fileProvider;
        _logger = logger;
        _peerConnectionLogger = peerConnectionLogger;
        _downloaderLogger = downloaderLogger;
        _decoder = decoder;
        _torrentService = torrentService;
        _peerListener = peerListener;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var contents = _fileProvider.GetDirectoryContents("");
        var torrentFile = contents
            .Where(fileInfo => fileInfo.IsDirectory == false && fileInfo.Name.EndsWith(".torrent"))
            .Select(fileInfo => fileInfo.Name)
            .FirstOrDefault();
        
        // read torrent file as stream
        var allBytes = await System.IO.File.ReadAllBytesAsync(torrentFile ?? throw new InvalidOperationException("No torrent file found"));
        var decoded = _decoder.DecodeFromBytes(allBytes);
        
        var torrent = _torrentService.ConvertToTorrent(decoded);

        var trackerResponse = await _torrentService.GetPeers(torrent, TrackerEvent.Started);

        if (!trackerResponse.Success)
        {
            return BadRequest(new { error = trackerResponse.FailureReason });
        }
        _logger.LogInformation("Tracker returned {PeerCount} bytes of peer data, re-announce in {Interval}s", 
            trackerResponse.PeersData.Length, 
            trackerResponse.Interval);

        _torrentService.SetPeers(torrent, trackerResponse.PeersData);

        foreach (var peer in torrent.Peers)
        {
            _logger.LogInformation("Trying peer {Ip}:{Port}", peer.IpAddress, peer.Port);
            
            var result = await _peerListener.QueueHandshakeAsync(
                peer.IpAddress, peer.Port, Client.PeerId, torrent.InfoHash);
            
            if (result.Success)
            {
                return Ok(result.Response);
            }
            
            _logger.LogWarning("Peer {Ip}:{Port} failed: {Error}", 
                peer.IpAddress, peer.Port, result.Error);
        }
        
        return BadRequest(new { error = "Could not connect to any peers" });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadTorrent(IFormFile torrentFile)
    {
        // Step 1: Validate the file
        if (torrentFile == null || torrentFile.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }
        
        if (!torrentFile.FileName.EndsWith(".torrent"))
        {
            return BadRequest(new { error = "File must be a .torrent file" });
        }
        
        // Step 2: Read file bytes
        using var memoryStream = new MemoryStream();
        await torrentFile.CopyToAsync(memoryStream);
        var allBytes = memoryStream.ToArray();
        
        // Step 3: Parse torrent and get peers
        var decoded = _decoder.DecodeFromBytes(allBytes);
        var torrent = _torrentService.ConvertToTorrent(decoded);

        var trackerResponse = await _torrentService.GetPeers(torrent, TrackerEvent.Started);

        if (!trackerResponse.Success)
        {
            return BadRequest(new { error = trackerResponse.FailureReason });
        }
        
        _logger.LogInformation("Tracker returned {PeerCount} bytes of peer data, re-announce in {Interval}s", 
            trackerResponse.PeersData.Length, 
            trackerResponse.Interval);

        _torrentService.SetPeers(torrent, trackerResponse.PeersData);

        if (torrent.Peers.Count == 0)
        {
            return BadRequest(new { error = "No peers available" });
        }

        // Step 4: Create downloader
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
        var downloader = new TorrentDownloader(torrent, outputDir, _downloaderLogger);
        
        // Step 5: Try connecting to peers
        foreach (var peer in torrent.Peers)
        {
            try
            {
                _logger.LogInformation("Connecting to peer {Ip}:{Port}", peer.IpAddress, peer.Port);
                
                await using var connection = await PeerConnection.ConnectAsync(
                    peer.IpAddress, 
                    peer.Port, 
                    Client.PeerId, 
                    torrent.InfoHash,
                    _peerConnectionLogger);
                
                await connection.SendInterestedAsync();
                
                await connection.RunMessageLoopAsync(
                    async (conn, message) => await downloader.HandleMessageAsync(conn, message),
                    downloader.CompletionToken);
                
                if (downloader.IsComplete)
                {
                    return Ok(new 
                    { 
                        success = true,
                        message = "Download complete!",
                        file = Path.Combine(outputDir, torrent.Info.Name ?? "download"),
                        pieces = downloader.TotalPieces
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Peer {Ip}:{Port} failed: {Error}", 
                    peer.IpAddress, peer.Port, ex.Message);
            }
        }
        
        return Ok(new 
        { 
            success = downloader.IsComplete,
            completed = downloader.CompletedPieceCount,
            total = downloader.TotalPieces,
            message = downloader.IsComplete ? "Download complete!" : "Download incomplete - ran out of peers"
        });
    }
}

