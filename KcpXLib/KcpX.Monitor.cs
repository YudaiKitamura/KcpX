using KcpXLib.Commands;
using KcpXLib.Exceptions;

namespace KcpXLib;

public partial class KcpX
{
    private int monitorGeneration;

    /// <summary>
    /// モニタ登録
    /// </summary>
    /// <remarks>
    /// <see cref="MonitorBuilder"/> に追加したデバイスを PLC にモニタ登録（コマンド: MBS / MWS）し、読出し用のセッションを返します。<br/>
    /// 新たにモニタ登録すると、以前に作成したセッションは使用できなくなります。
    /// </remarks>
    /// <param name="configure">モニタ対象のデバイスとコールバックを <see cref="MonitorBuilder"/> に追加する処理を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">デバイスを 1 つも追加しない場合、またはビット・ワードのいずれかで 120 点を超えて追加した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">モニタできないデバイスと型の組み合わせを指定した場合、または接続先がモニタに対応していない場合（<see cref="KcpXSimulator"/>）にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>モニタセッションを返します。</returns>
    public MonitorSession MonitorRegist(Action<MonitorBuilder> configure)
    {
        var builder = CreateMonitorBuilder(configure);
        int generation = Interlocked.Increment(ref monitorGeneration);
        if (builder.bitEntries.Count > 0)
        {
            new PlcCommandHandler<bool>().Execute(CreateBitRegistCommand(builder), this);
        }
        if (builder.wordPoints.Count > 0)
        {
            new PlcCommandHandler<bool>().Execute(CreateWordRegistCommand(builder), this);
        }
        return new MonitorSession(this, builder, generation);
    }

    /// <inheritdoc cref="MonitorRegist(Action{MonitorBuilder})"/>
    /// <summary>
    /// モニタ登録（非同期）
    /// </summary>
    public async Task<MonitorSession> MonitorRegistAsync(Action<MonitorBuilder> configure)
    {
        var builder = CreateMonitorBuilder(configure);
        int generation = Interlocked.Increment(ref monitorGeneration);
        if (builder.bitEntries.Count > 0)
        {
            await new PlcCommandHandler<bool>().ExecuteAsync(CreateBitRegistCommand(builder), this);
        }
        if (builder.wordPoints.Count > 0)
        {
            await new PlcCommandHandler<bool>().ExecuteAsync(CreateWordRegistCommand(builder), this);
        }
        return new MonitorSession(this, builder, generation);
    }

    /// <summary>
    /// 接続先がモニタ（MBS / MBR / MWS / MWR）に対応していない場合に <see cref="NotSupportedException"/> をスローする。
    /// </summary>
    private protected virtual void EnsureMonitorSupported()
    {
    }

    private MonitorBuilder CreateMonitorBuilder(Action<MonitorBuilder> configure)
    {
        EnsureMonitorSupported();
        var builder = new MonitorBuilder();
        configure(builder);
        if (builder.bitEntries.Count == 0 && builder.wordEntries.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(configure), "Add at least one device to monitor.");
        }
        return builder;
    }

    private static MonitorBitRegistCommand CreateBitRegistCommand(MonitorBuilder builder) =>
        new(builder.bitEntries.Select(e => (e.prefix, e.address)).ToArray());

    private static MonitorWordRegistCommand CreateWordRegistCommand(MonitorBuilder builder) =>
        new(builder.wordPoints.ToArray());

    internal bool[] MonitorBitRead(int generation, int count)
    {
        EnsureCurrentMonitor(generation);
        return new PlcCommandHandler<bool[]>().Execute(new MonitorBitReadCommand(count), this);
    }

    internal async Task<bool[]> MonitorBitReadAsync(int generation, int count)
    {
        EnsureCurrentMonitor(generation);
        return await new PlcCommandHandler<bool[]>().ExecuteAsync(new MonitorBitReadCommand(count), this);
    }

    internal string[] MonitorWordRead(int generation, int count)
    {
        EnsureCurrentMonitor(generation);
        return new PlcCommandHandler<string[]>().Execute(new MonitorWordReadCommand(count), this);
    }

    internal async Task<string[]> MonitorWordReadAsync(int generation, int count)
    {
        EnsureCurrentMonitor(generation);
        return await new PlcCommandHandler<string[]>().ExecuteAsync(new MonitorWordReadCommand(count), this);
    }

    private void EnsureCurrentMonitor(int generation)
    {
        if (generation != Volatile.Read(ref monitorGeneration))
        {
            throw new InvalidOperationException("The monitor registration was replaced by a later MonitorRegist call.");
        }
    }
}
