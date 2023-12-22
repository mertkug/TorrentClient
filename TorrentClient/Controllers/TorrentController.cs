using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using TorrentClient.Bencode;
using TorrentClient.Services;
using Encoder = TorrentClient.Bencode.Encoder;
using System.IO;
using System; 
namespace TorrentClient.Controllers;

[ApiController]
[Route("/api/")]
public class TorrentController : ControllerBase
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<TorrentController> _logger;
    private readonly IDecoder _decoder;
    private readonly ITorrentService _torrentService;

    public TorrentController(ILogger<TorrentController> logger, IFileProvider fileProvider, IDecoder decoder, ITorrentService torrentService)
    {
        _fileProvider = fileProvider;
        _logger = logger;
        _decoder = decoder;
        _torrentService = torrentService;
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
        // Console.WriteLine(decoded);
        // allBytes[104..233]
        var torrent = _torrentService.ConvertToTorrent(decoded, allBytes[104..233]);
        Console.WriteLine(torrent);
        return Ok(torrent.InfoHash);
    }
}