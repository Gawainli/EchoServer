using System.Net;
using System.Net.Sockets;
using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class TcpEchoServer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly DefaultPkgDecoder _pkgDecoder = new();
    private readonly DefaultPkgEncoder _pkgEncoder = new();
    private readonly EchoMessageEncoder _echoMessageEncoder = new();
    private readonly EchoMessageDecoder _echoMessageDecoder = new();

    public async Task Start(string address, int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Parse(address), port);
        listener.Start();
        Console.WriteLine($"Socket Server started at {address}:{port}");
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Socket Client connected");
                Console.WriteLine($"Socket Remote endpoint: {client.Client.RemoteEndPoint}");
                await HandleTcpClientAsync(client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Socket server stopped.");
            Console.WriteLine(e);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleTcpClientAsync(TcpClient tcpClient)
    {
        var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
        var stream = tcpClient.GetStream();
        try
        {
            while (tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1);
                // if (!stream.DataAvailable)
                // {
                //     continue;
                // }

                var read = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                if (read == 0)
                {
                    break;
                }
                
                _pkgDecoder.Buffer.Write(buffer, 0, read);
                while (_pkgDecoder.Decode() is DefaultNetPackage netPkg)
                {
                    if (netPkg.MsgId == 1)
                    {
                        Console.WriteLine("Socket Received Heartbeat Message.");
                        continue;
                    }
                    if (_echoMessageDecoder.Decode(netPkg.BodyBytes) is EchoMessage echoMessage)
                    {
                        Console.WriteLine("Socket Received Echo Message: " + echoMessage.text);
                        await SendEchoMessagePkg(stream, $"Socket Server Echo: {echoMessage.text}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            _cts.Cancel();
            Console.WriteLine(e);
        }
        finally
        {
            Console.WriteLine("Socket Client disconnected.");
            tcpClient.Close();
            tcpClient.Dispose();
        }
    }

    private async Task SendEchoMessagePkg(NetworkStream stream, string text)
    {
        var buffer = EchoPkgHelper.GetEchoPkgBytes(text);
        await stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
    }
}