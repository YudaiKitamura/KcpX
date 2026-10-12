using System.Net;
using System.Net.Sockets;
using KcpXLib.Interfaces;
using KcpXLib.Utils;

namespace KcpXLib.Transports;

internal class UdpPlcTransport : IPlcTransport
{
    private const string TimeoutMessage = "The request timed out.";

    private UdpClient udp;
    private readonly IPEndPoint remoteEndPoint;
    private readonly ushort timeout;
    private readonly SemaphoreSlim gate = new(1, 1);

    internal UdpPlcTransport(string ip, int port, ushort timeout)
    {
        this.timeout = timeout;
        udp = CreateClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    private UdpClient CreateClient()
    {
        var client = new UdpClient();
        client.Client.ReceiveTimeout = timeout;
        return client;
    }

    // 遅れて届いた応答を次の要求が受け取らないよう、タイムアウト時はソケットを作り直す。
    private void RecreateClient()
    {
        udp.Dispose();
        udp = CreateClient();
    }

    public byte[] Request(byte[] packet)
    {
        gate.Wait();
        try
        {
            udp.Send(packet, packet.Length, remoteEndPoint);

            IPEndPoint remote = remoteEndPoint;
            try
            {
                return udp.Receive(ref remote);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                RecreateClient();
                throw new TimeoutException(TimeoutMessage, ex);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        await gate.WaitAsync();
        try
        {
            await udp.SendAsync(packet, packet.Length, remoteEndPoint);

            using var cts = new CancellationTokenSource();
            var receiveTask = udp.ReceiveAsync();
            var delayTask = Task.Delay(timeout == 0 ? Timeout.Infinite : timeout, cts.Token);

            if (await Task.WhenAny(receiveTask, delayTask) == delayTask)
            {
                receiveTask.ObserveException();
                RecreateClient();
                throw new TimeoutException(TimeoutMessage, new SocketException((int)SocketError.TimedOut));
            }

            cts.Cancel();
            return (await receiveTask).Buffer;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        udp.Dispose();
    }
}
