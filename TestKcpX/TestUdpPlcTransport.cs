using System.Net;
using System.Net.Sockets;
using System.Text;
using KcpXLib.Transports;

namespace TestKcpX;

[TestClass]
public sealed class TestUdpPlcTransport
{
    private static (int Port, CancellationTokenSource Cts) StartServer(string response, int delayMs)
    {
        var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var received = await server.ReceiveAsync(cts.Token);
                    await Task.Delay(delayMs, cts.Token);
                    var bytes = Encoding.ASCII.GetBytes(response);
                    await server.SendAsync(bytes, received.RemoteEndPoint, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                server.Dispose();
            }
        });

        return (port, cts);
    }

    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public async Task TestRequest()
    {
        var (port, cts) = StartServer("OK\r\n", 0);
        using var transport = new UdpPlcTransport("127.0.0.1", port, 3000);

        CollectionAssert.AreEqual(Bytes("OK\r\n"), transport.Request(Bytes("WR DM0 1\r")));
        CollectionAssert.AreEqual(Bytes("OK\r\n"), await transport.RequestAsync(Bytes("WR DM0 1\r")));
        cts.Cancel();
    }

    [TestMethod]
    public async Task TestTimeout()
    {
        var (port, cts) = StartServer("OK\r\n", 1000);
        using var transport = new UdpPlcTransport("127.0.0.1", port, 200);

        Assert.ThrowsException<TimeoutException>(() => transport.Request(Bytes("WR DM0 1\r")));
        await Assert.ThrowsExceptionAsync<TimeoutException>(() => transport.RequestAsync(Bytes("WR DM0 1\r")));
        cts.Cancel();
    }
}
