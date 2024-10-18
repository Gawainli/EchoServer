// See https://aka.ms/new-console-template for more information
using EchoTCPServerCSharp.Server;

if (args.Length < 2)
{
    Console.WriteLine("Usage: EchoTCPServerCSharp <serverType(ws or tcp)> <address> <port>");
    return;
}

var serverType = args[0];
var address = args[1];
var port = int.Parse(args[2]);

switch (serverType)
{
    case "ws":
    {
        Console.WriteLine("Run Echo WebSocket Server");
        var wsEchoServer = new WsEchoServer();
        await wsEchoServer.Start($"http://{address}:{port}/ws/");
        break;
    }
    case "tcp":
    {
        Console.WriteLine("Run Echo TCP Server");
        var tcpEchoServer = new TcpEchoServer();
        await tcpEchoServer.Start(address, port);
        break;
    }
    default:
        Console.WriteLine("Usage: EchoTCPServerCSharp <serverType(ws or tcp)> <address> <port>");
        break;
}