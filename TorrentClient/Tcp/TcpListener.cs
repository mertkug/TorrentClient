using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TorrentClient.Tcp;

public class TcpListener
{
    private NetworkStream stream;
    
    /// <summary>
    /// Performs handshake with peer
    /// </summary>
    /// <param name="peerPort">Port of peer</param>
    /// <param name="peerIp">IP Address of peer</param>
    /// <param name="peerId">Unique id of client</param>
    /// <param name="infoHash">info hash of torrent in HEX format</param>
    /// <returns>response in byte stream</returns>
    /// <exception cref="Exception">Throws when handshake failed</exception>
    public byte[] PerformHandShake(int peerPort, IPAddress peerIp, string peerId, string infoHash)
    {
        using var client = new TcpClient(peerIp.ToString(), peerPort);
        stream = client.GetStream();
        var handshake = new byte[68];

        handshake[0] = 19;
        var protocol = "BitTorrent protocol"u8.ToArray();
        Buffer.BlockCopy(protocol, 0, handshake, 1, protocol.Length);
        var reserved = new byte[8];
        Buffer.BlockCopy(reserved, 0, handshake, 20, reserved.Length);
        Buffer.BlockCopy(Convert.FromHexString(infoHash), 0, handshake, 28, 20);
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(peerId), 0, handshake, 48, 20);
        stream.Write(handshake, 0, handshake.Length);
        
        var response = new byte[68];
        var read = stream.Read(response, 0, response.Length);
        
        if (read != 68)
        {
            throw new Exception("Handshake failed");
        }

        return response;
    }
    
    /// <summary>
    /// Parse response from peer
    /// </summary>
    /// <param name="response">
    /// Response from peer
    /// </param>
    /// <returns>
    /// Anonymous type structured as: { protocol: string, infoHash: string, peerId: string }
    /// </returns>
    public static object ParseResponse(byte[] response)
    {
        var protocol = Encoding.UTF8.GetString(response[1..20]);
        // 8 bytes after this reserved, but it's all 0s, so we can ignore it
        var infoHash = Convert.ToHexString(response[28..48]);
        var peerId = Convert.ToHexString(response[48..]);
        return new
        {
            protocol,
            infoHash,
            peerId
        };
    }
}