// See https://aka.ms/new-console-template for more information

using EchoTCPServerCSharp.Server;

Console.WriteLine("Run Echo TCP Server");
TcpEchoServer.Run("127.0.0.1", 8893);