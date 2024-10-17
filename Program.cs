// See https://aka.ms/new-console-template for more information

using EchoTCPServerCSharp.Server;

// Console.WriteLine("Run Echo TCP Server");
// var tcpEchoServer = new TcpEchoServer();
// tcpEchoServer.Start("127.0.0.1", 8893);
// await Task.Run(async () => { await tcpEchoServer.Start("127.0.0.1", 8893); });

Console.WriteLine("Run Echo WebSocket Server");
var wsEchoServer = new WsEchoServer();
// await Task.Run(async () => { await wsEchoServer.Start("http://localhost:8080/ws/"); });
await wsEchoServer.Start("http://localhost:8080/ws/");