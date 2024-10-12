using System.Net;
using System.Net.Sockets;
using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class TcpEchoServer
{
    public static void Run(string address, int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Parse(address), port);
        listener.Start();
        Console.WriteLine($"Server started at {address}:{port}");

        try
        {
            while (true)
            {
                var client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected");
                Console.WriteLine($"Remote endpoint: {client.Client.RemoteEndPoint}");
                var channel = new TcpChannel(client, new DefaultPkgEncoder(), new DefaultPkgDecoder());
                channel.StartProcess();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}