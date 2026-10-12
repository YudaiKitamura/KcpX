using System.Diagnostics;
using System.Net.Sockets;
using KcpXLib.Interfaces;
using KcpXLib.Utils;

namespace KcpXLib.Transports;

internal class TcpPlcTransport : IPlcTransport
{
    private const string TimeoutMessage = "The request timed out.";

    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly SemaphoreSlim gate = new(1, 1);
    // 0 は無期限。
    private readonly ushort timeout;
    // タイムアウト・通信エラー後は応答がずれるため再利用しない。
    private bool broken;

    internal TcpPlcTransport(string ip, int port, ushort timeout)
    {
        this.timeout = timeout;
        client = new TcpClient
        {
            SendTimeout = timeout,
            ReceiveTimeout = timeout,
        };

        var task = client.ConnectAsync(ip, port);
        if (Task.WaitAny([task], timeout == 0 ? Timeout.Infinite : timeout) != 0)
        {
            task.ObserveException();
            client.Dispose();
            throw new TimeoutException("Connection Timeout");
        }

        if (task.IsFaulted)
        {
            client.Dispose();
            task.GetAwaiter().GetResult();
        }

        stream = client.GetStream();
    }

    public void Dispose()
    {
        Break();
    }

    public byte[] Request(byte[] packet)
    {
        gate.Wait();
        try
        {
            ThrowIfBroken();
            try
            {
                var elapsed = Stopwatch.StartNew();

                stream.WriteTimeout = GetRemaining(elapsed);
                stream.Write(packet, 0, packet.Length);

                var response = new MemoryStream();
                var buffer = new byte[1024];
                while (!EndsWithCrLf(response))
                {
                    stream.ReadTimeout = GetRemaining(elapsed);
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        throw new IOException("Connection closed unexpectedly");
                    }
                    response.Write(buffer, 0, bytesRead);
                }
                return response.ToArray();
            }
            catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut } inner)
            {
                Break();
                throw new TimeoutException(TimeoutMessage, inner);
            }
            catch
            {
                Break();
                throw;
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
            ThrowIfBroken();
            try
            {
                var elapsed = Stopwatch.StartNew();

                await WithDeadlineAsync(stream.WriteAsync(packet, 0, packet.Length), elapsed);

                var response = new MemoryStream();
                var buffer = new byte[1024];
                while (!EndsWithCrLf(response))
                {
                    int bytesRead = await WithDeadlineAsync(stream.ReadAsync(buffer, 0, buffer.Length), elapsed);
                    if (bytesRead == 0)
                    {
                        throw new IOException("Connection closed unexpectedly");
                    }
                    response.Write(buffer, 0, bytesRead);
                }
                return response.ToArray();
            }
            catch
            {
                Break();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool EndsWithCrLf(MemoryStream response)
    {
        if (response.Length < 2)
        {
            return false;
        }
        var bytes = response.GetBuffer();
        return bytes[response.Length - 2] == 0x0D && bytes[response.Length - 1] == 0x0A;
    }

    private void ThrowIfBroken()
    {
        if (broken)
        {
            throw new IOException("The connection was closed due to a previous error. Create a new instance to reconnect.");
        }
    }

    private void Break()
    {
        broken = true;
        stream.Dispose();
        client.Dispose();
    }

    private int GetRemaining(Stopwatch elapsed)
    {
        if (timeout == 0)
        {
            return Timeout.Infinite;
        }

        long remaining = timeout - elapsed.ElapsedMilliseconds;
        if (remaining <= 0)
        {
            throw CreateTimeoutException();
        }
        return (int)remaining;
    }

    // NetworkStream の非同期 I/O は ReceiveTimeout / SendTimeout を見ないため、期限切れを WhenAny で判定する。
    private async Task WithDeadlineAsync(Task task, Stopwatch elapsed)
    {
        int remaining = GetRemaining(elapsed);
        if (remaining == Timeout.Infinite)
        {
            await task;
            return;
        }

        using var cts = new CancellationTokenSource();
        var delay = Task.Delay(remaining, cts.Token);

        if (await Task.WhenAny(task, delay) != task)
        {
            task.ObserveException();
            throw CreateTimeoutException();
        }

        cts.Cancel();
        await task;
    }

    private async Task<T> WithDeadlineAsync<T>(Task<T> task, Stopwatch elapsed)
    {
        await WithDeadlineAsync((Task)task, elapsed);
        return await task;
    }

    private static TimeoutException CreateTimeoutException()
    {
        return new TimeoutException(TimeoutMessage, new SocketException((int)SocketError.TimedOut));
    }
}
