using System.Net;
using System.Net.WebSockets;
using System.Text;
using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class WsEchoServer
{
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
                    // HandleWebSocketAsync(webSocket);
                    await Task.Run(() => HandleWebSocketAsync(webSocket));
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
            Console.WriteLine(e);
        }
    }

    private async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
        var handleCts = new CancellationTokenSource();
        var receivedCout = 0;
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result =
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), handleCts.Token);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", handleCts.Token);
                        Console.WriteLine("WebSocket Connection closed.");
                        break;
                    case WebSocketMessageType.Binary:
                        _pkgDecoder.Buffer.Write(buffer, 0, result.Count);
                        while (_pkgDecoder.Decode() is DefaultNetPackage netPkg)
                        {
                            if (netPkg.MsgId == 1)
                            {
                                Console.WriteLine("WebSocket Received Heartbeat Message.");
                                await SendPingPkg(webSocket, handleCts.Token);
                                continue;
                            }

                            if (_echoMessageDecoder.Decode(netPkg.BodyBytes) is not EchoMessage echoMessage) continue;
                            Console.WriteLine($"WebSocket Received Echo Message {receivedCout++}: {echoMessage.text}");
                            await SendEchoMessagePkg(webSocket, $"WebSocket Server Echo: {echoMessage.text}",
                                handleCts.Token);
                        }

                        break;
                    case WebSocketMessageType.Text:
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine("WebSocket Received Text Message: " + message);
                        await SendEchoMessage(webSocket, message, handleCts.Token);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("WebSocket Connection closing.");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", handleCts.Token);
        }
        finally
        {
            handleCts.Cancel();
            webSocket.Dispose();
            Console.WriteLine($"WebSocket Connection closed.");
        }
    }

    private async Task SendEchoMessagePkg(WebSocket webSocket, string text, CancellationToken token)
    {
        var echoPkg = EchoPkgHelper.GetEchoPkgBytes(text);
        await webSocket.SendAsync(new ArraySegment<byte>(echoPkg), WebSocketMessageType.Binary, true, token);
    }

    private async Task SendPingPkg(WebSocket webSocket, CancellationToken token)
    {
        var bytes = EchoPkgHelper.GetPingPkgBytes();
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, token);
    }

    private async Task SendEchoMessage(WebSocket webSocket, string text, CancellationToken token)
    {
        var responseBytes = Encoding.UTF8.GetBytes("Server Echo: " + text);
        await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, token);
    }
}