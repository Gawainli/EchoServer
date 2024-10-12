using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MiniGame.Network
{
    public class TcpTestChannel : IDisposable
    {
        private const float HeartBeatInterval = 1f;
        
        private readonly TcpClient _tcpClient;
        private readonly ConcurrentQueue<DefaultNetPackage> _sendQueue = new ConcurrentQueue<DefaultNetPackage>();
        private readonly ConcurrentQueue<DefaultNetPackage> _receiveQueue = new ConcurrentQueue<DefaultNetPackage>();

        private readonly RingBuffer _encodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);
        private readonly RingBuffer _decodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);

        private readonly INetPackageEncoder _encoder;
        private readonly INetPackageDecoder _decoder;
        
        private float _heartBeatWaitTime = 0f;

        public TcpTestChannel(TcpClient tcpClient, INetPackageEncoder encoder, INetPackageDecoder decoder)
        {
            _tcpClient = tcpClient;
            _encoder = encoder;
            _decoder = decoder;
        }

        public void Start()
        {
            Task.Run(SendProcess);
            // Task.Run(TestReceiveProcess);
            Task.Run(ReceiveProcess);
        }

        public INetPackage? PickPkg()
        {
            if (_receiveQueue.TryDequeue(out var pkg))
            {
                return pkg;
            }

            return null;
        }
        
        public void SendPkg(DefaultNetPackage pkg)
        {
            _sendQueue.Enqueue(pkg);
        }

        private async void SendProcess()
        {
            var stream = _tcpClient.GetStream();
            while (_tcpClient.Connected)
            {
                Thread.Sleep(1);
                while (_sendQueue.Count > 0)
                {
                    if (_sendQueue.TryDequeue(out var pkg))
                    {
                        if (_encodeBuffer.WriteableBytes < DefaultNetPackage.PkgMaxSize)
                        {
                            break;
                        }

                        _encoder.Encode(_encodeBuffer, pkg);
                    }
                }

                if (_encodeBuffer.ReadableBytes > 0)
                {
                    var bytes = _encodeBuffer.ReadBytes(_encodeBuffer.ReadableBytes);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }

        private async void TestReceiveProcess()
        {
            var stream = _tcpClient.GetStream();
            var buffer = new byte[8192];
            var bytesRead = 0;

            while (_tcpClient.Connected)
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    continue;
                }

                var data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {data}");

                var response = System.Text.Encoding.UTF8.GetBytes(data);
                stream.Write(response, 0, response.Length);
            }
        }

        private async void ReceiveProcess()
        {
            var stream = _tcpClient.GetStream();
            var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
            var tempPackages = new List<INetPackage>();
            while (_tcpClient.Connected)
            {
                if (!stream.DataAvailable) continue;
                var recvBytesCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (recvBytesCount == 0 || !_decodeBuffer.IsWriteable(recvBytesCount))
                {
                    Console.WriteLine("ReceiveProcess: recvBytesCount == 0");
                    continue;
                }

                _decodeBuffer.WriteBytes(buffer, 0, recvBytesCount);
                _decoder.Decode(_decodeBuffer, tempPackages);
                foreach (var pkg in tempPackages)
                {
                    var netPkg = (DefaultNetPackage)pkg;
                    Console.WriteLine(
                        $"Receive pkg. msgId: {netPkg.MsgId} time: {DateTime.Now:HH:mm:ss.fff}");
                    if (netPkg.MsgId == 1)
                    {
                    }
                    _receiveQueue.Enqueue(netPkg);
                }

                tempPackages.Clear();
            }
        }

        public void Dispose()
        {
            _tcpClient.GetStream().Close();
            _tcpClient.Dispose();
        }
    }
}