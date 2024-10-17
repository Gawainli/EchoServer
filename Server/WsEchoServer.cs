using System.Net;
using System.Net.WebSockets;
using System.Text;
using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class WsEchoServer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly DefaultPkgDecoder _pkgDecoder = new();
    private readonly EchoMessageDecoder _echoMessageDecoder = new();

    public async Task Start(string uriPrefix)
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(uriPrefix);
        httpListener.Start();
        Console.WriteLine($"WebSocket server started at {uriPrefix}");
        Console.WriteLine($"WebSocket client connected at {uriPrefix.Replace("http:", "ws:")}");
        try
        {
            while (true)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                Console.WriteLine(
                    $"HttpListenerContext received. " +
                    $"IsWebSocketRequest: {context.Request.IsWebSocketRequest} " +
                    $"Ip: {context.Request.RemoteEndPoint}");
                if (context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    var webSocket = webSocketContext.WebSocket;
                    Console.WriteLine("Client connected.");
                    // Handle WebSocket communication
                    await HandleWebSocketAsync(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("WebSocket server stopped.");
            Console.WriteLine(e);
        }
        finally
        {
            httpListener.Stop();
        }
    }

    private async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result =
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
                        Console.WriteLine("WebSocket Connection closed.");
                        break;
                    case WebSocketMessageType.Binary:
                        _pkgDecoder.Buffer.Write(buffer, 0, result.Count);
                        while (_pkgDecoder.Decode() is DefaultNetPackage netPkg)
                        {
                            if (netPkg.MsgId == 1)
                            {
                                Console.WriteLine("WebSocket Received Heartbeat Message.");
                                continue;
                            }
                            if (_echoMessageDecoder.Decode(netPkg.BodyBytes) is not EchoMessage echoMessage) continue;
                            Console.WriteLine("WebSocket Received Echo Message: " + echoMessage.text);
                            await SendEchoMessagePkg(webSocket, $"WebSocket Server Echo: {echoMessage.text}");
                        }

                        break;
                    case WebSocketMessageType.Text:
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine("WebSocket Received Text Message: " + message);
                        await SendEchoMessage(webSocket, message);
                        break;
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
            Console.WriteLine($"Connection closed.");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
            webSocket.Dispose();
        }
    }

    private async Task SendEchoMessagePkg(WebSocket webSocket, string text)
    {
        var echoPkg = EchoPkgHelper.GetEchoPkgBytes(text);
        await webSocket.SendAsync(new ArraySegment<byte>(echoPkg), WebSocketMessageType.Binary, true, _cts.Token);
    }

    private async Task SendEchoMessage(WebSocket webSocket, string text)
    {
        var responseBytes = Encoding.UTF8.GetBytes("Server Echo: " + text);
        await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, _cts.Token);
    }
}