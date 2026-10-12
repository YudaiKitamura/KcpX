using System.Net;
using System.Net.Sockets;
using System.Text;
using KcpXLib.Transports;

namespace TestKcpX;

[TestClass]
public sealed class TestTcpPlcTransport
{
    /// <summary>
    /// CR までのコマンドを受信するたびに、<paramref name="respond"/> が返すチャンクを順に送信するサーバを起動する。
    /// </summary>
    private static (int Port, CancellationTokenSource Cts) StartServer(Func<string, IEnumerable<(string Chunk, int DelayMs)>> respond)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync(cts.Token);
                using var stream = client.GetStream();
                var command = new StringBuilder();
                var buffer = new byte[1];
                while (await stream.ReadAsync(buffer, cts.Token) > 0)
                {
                    if (buffer[0] != '\r')
                    {
                        command.Append((char)buffer[0]);
                        continue;
                    }
                    foreach (var (chunk, delay) in respond(command.ToString()))
                    {
                        await Task.Delay(delay, cts.Token);
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(chunk), cts.Token);
                    }
                    command.Clear();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException)
            {
            }
            finally
            {
                listener.Stop();
            }
        });

        return (port, cts);
    }

    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestRequest()
    {
        var (port, cts) = StartServer(command => [(command == "RD DM0" ? "00123\r\n" : "E1\r\n", 0)]);
        using var transport = new TcpPlcTransport("127.0.0.1", port, 3000);

        CollectionAssert.AreEqual(Bytes("00123\r\n"), transport.Request(Bytes("RD DM0\r")));
        CollectionAssert.AreEqual(Bytes("E1\r\n"), transport.Request(Bytes("XX\r")));
        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsyncSplitResponse()
    {
        var (port, cts) = StartServer(_ => [("1 0 ", 0), ("1\r", 50), ("\n", 50)]);
        using var transport = new TcpPlcTransport("127.0.0.1", port, 3000);

        CollectionAssert.AreEqual(Bytes("1 0 1\r\n"), await transport.RequestAsync(Bytes("RDS R0 3\r")));
        CollectionAssert.AreEqual(Bytes("1 0 1\r\n"), transport.Request(Bytes("RDS R0 3\r")));
        cts.Cancel();
    }

    [TestMethod]
    public void TestTimeoutBreaksConnection()
    {
        var (port, cts) = StartServer(_ => [("OK\r\n", 1000)]);
        using var transport = new TcpPlcTransport("127.0.0.1", port, 200);

        Assert.ThrowsException<TimeoutException>(() => transport.Request(Bytes("WR DM0 1\r")));
        Assert.ThrowsException<IOException>(() => transport.Request(Bytes("WR DM0 1\r")));
        cts.Cancel();
    }

    [TestMethod]
    public async Task TestTimeoutAsync()
    {
        var (port, cts) = StartServer(_ => [("OK\r\n", 1000)]);
        using var transport = new TcpPlcTransport("127.0.0.1", port, 200);

        await Assert.ThrowsExceptionAsync<TimeoutException>(() => transport.RequestAsync(Bytes("WR DM0 1\r")));
        await Assert.ThrowsExceptionAsync<IOException>(() => transport.RequestAsync(Bytes("WR DM0 1\r")));
        cts.Cancel();
    }

    [TestMethod]
    public void TestConnectionRefused()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.ThrowsException<SocketException>(() => new TcpPlcTransport("127.0.0.1", port, 1000));
    }
}
