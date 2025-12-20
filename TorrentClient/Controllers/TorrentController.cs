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

namespace TorrentClient.Controllers;

[ApiController]
[Route("/api/")]
public class TorrentController : ControllerBase
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<TorrentController> _logger;
    private readonly IDecoder _decoder;
    private readonly TorrentService _torrentService;
    private readonly PeerListener _peerListener;

    public TorrentController(ILogger<TorrentController> logger, 
        IFileProvider fileProvider, 
        IDecoder decoder,
        TorrentService torrentService,
        PeerListener peerListener)
    {
        _fileProvider = fileProvider;
        _logger = logger;
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
        var allBytes = await System.IO.File.ReadAllBytesAsync(torrentFile);
        var decoded = _decoder.DecodeFromBytes(allBytes);
        
        var torrent = _torrentService.ConvertToTorrent(decoded);
        _torrentService.SetPeers(torrent, await _torrentService.GetPeers(torrent));
        
        var peer = torrent.Peers[0];
        var result = await _peerListener.QueueHandshakeAsync(peer.IpAddress, peer.Port, Client.PeerId, torrent.InfoHash);
        
        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }
        
        return Ok(result.Response);
    }
}