using MiniGame.Network;

namespace EchoTCPServerCSharp.Server;

public class EchoPkgHelper
{
    private static readonly DefaultNetPackage EchoPkg = new()
        { MsgId = 2, MsgIndex = -1, BodyBytes = new byte[] { } };

    private static readonly DefaultNetPackage PingPkg = new()
        { MsgId = 1, MsgIndex = -1, BodyBytes = new byte[] { } };

    private static readonly EchoMessageEncoder EchoMsgEncoder = new();
    private static readonly DefaultPkgEncoder PkgEncoder = new();
    private static byte[] PingPkgBytes;

    public static byte[] GetEchoPkgBytes(string text)
    {
        var echoMessage = new EchoMessage() { text = text };
        var bodyBytes = EchoMsgEncoder.Encode(echoMessage);
        EchoPkg.BodyBytes = bodyBytes;
        PkgEncoder.Encode(EchoPkg);
        return PkgEncoder.Buffer.ReadAllAvailable();
    }

    private static byte[] GetPingPkgBytes()
    {
        if (PingPkgBytes == null)
        {
            PkgEncoder.Encode(PingPkg);
            PingPkgBytes = PkgEncoder.Buffer.ReadAllAvailable();
        }

        return PingPkgBytes;
    }
}