using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using TorrentClient.Bencode;
using TorrentClient.Models;

namespace TorrentClient.Controllers;

[ApiController]
[Route("/api/")]
public class TorrentController : ControllerBase
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<TorrentController> _logger;
    private readonly IParser _parser;

    public TorrentController(ILogger<TorrentController> logger, IFileProvider fileProvider, IParser parser)
    {
        _fileProvider = fileProvider;
        _logger = logger;
        _parser = parser;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var contents = _fileProvider.GetDirectoryContents("");
        var torrentFile = contents
            .Where(fileInfo => fileInfo.IsDirectory == false && fileInfo.Name.EndsWith(".torrent"))
            .Select(fileInfo => fileInfo.Name)
            .FirstOrDefault();

        if (torrentFile == null)
        {
            return NotFound();
        }
        var stream = _fileProvider.GetFileInfo(torrentFile).CreateReadStream();
        var decoded = _parser.Decode(stream);
        return Ok(decoded);
    }
    

}