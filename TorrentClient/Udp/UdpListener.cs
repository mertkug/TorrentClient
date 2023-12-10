using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TorrentClient.Udp;

public class UdpListener
{
    private static int _listenPort;

    public UdpListener(int listenPort)
    {
        _listenPort = listenPort;
    }
    
    private static void StartListener()
    {
        var listener = new UdpClient(_listenPort);
        var groupEp = new IPEndPoint(IPAddress.Any, _listenPort);

        try
        {
            while (true)
            {
                Console.WriteLine("Waiting for broadcast");
                var bytes = listener.Receive(ref groupEp);

                Console.WriteLine($"Received broadcast from {groupEp} :");
                Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            listener.Close();
        }
    }
}