using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class EchoPkgHelper
{
    private static readonly DefaultNetPackage EchoPkg = new()
        { MsgId = 2, MsgIndex = -1, BodyBytes = new byte[] { } };

    private static readonly DefaultNetPackage PingPkg = new()
        { MsgId = 1, MsgIndex = -1, BodyBytes = new byte[] { 1 } };

    private static readonly EchoMessageEncoder EchoMsgEncoder = new();
    private static readonly DefaultPkgEncoder PkgEncoder = new();
    private static byte[]? _pingPkgBytes;

    public static byte[] GetEchoPkgBytes(string text)
    {
        var echoMessage = new EchoMessage() { text = text };
        var bodyBytes = EchoMsgEncoder.Encode(echoMessage);
        EchoPkg.BodyBytes = bodyBytes;
        PkgEncoder.Encode(EchoPkg);
        return PkgEncoder.Buffer.ReadAllAvailable();
    }

    public static byte[] GetPingPkgBytes()
    {
        if (_pingPkgBytes != null) return _pingPkgBytes;
        PkgEncoder.Encode(PingPkg);
        _pingPkgBytes = PkgEncoder.Buffer.ReadAllAvailable();
        return _pingPkgBytes;
    }
}