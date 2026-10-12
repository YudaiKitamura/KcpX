using KcpXLib.Exceptions;

namespace KcpXLib;

/// <summary>
/// モニタセッション
/// </summary>
/// <remarks>
/// <see cref="KcpX.MonitorRegist(Action{MonitorBuilder})"/> で登録したデバイスを、
/// <see cref="Read"/> / <see cref="ReadAsync"/> で何度でも読み出せます。<br/>
/// PLC の再起動などで登録内容が消去された場合（読出し時に E0）は、再度 <see cref="KcpX.MonitorRegist(Action{MonitorBuilder})"/> で登録してください。
/// </remarks>
public sealed class MonitorSession
{
    private readonly KcpX kcpX;
    private readonly int generation;
    private readonly Action<bool>[] bitApplies;
    private readonly (int points, Action<string[]> apply)[] wordEntries;
    private readonly int wordPointCount;

    internal MonitorSession(KcpX kcpX, MonitorBuilder builder, int generation)
    {
        this.kcpX = kcpX;
        this.generation = generation;
        bitApplies = builder.bitEntries.Select(e => e.apply).ToArray();
        wordEntries = builder.wordEntries.ToArray();
        wordPointCount = builder.wordPoints.Count;
    }

    /// <summary>
    /// 登録済みのデバイスの値を読み出し（コマンド: MBR / MWR）、各コールバックへ値を渡します。
    /// </summary>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <exception cref="InvalidOperationException">このセッションの作成後に別のモニタ登録が行われた場合にスローします。</exception>
    public void Read()
    {
        if (bitApplies.Length > 0)
        {
            Dispatch(bitApplies, kcpX.MonitorBitRead(generation, bitApplies.Length));
        }
        if (wordEntries.Length > 0)
        {
            DispatchWords(kcpX.MonitorWordRead(generation, wordPointCount));
        }
    }

    /// <inheritdoc cref="Read"/>
    /// <summary>
    /// 登録済みのデバイスの値を非同期で読み出し（コマンド: MBR / MWR）、各コールバックへ値を渡します。
    /// </summary>
    public async Task ReadAsync()
    {
        if (bitApplies.Length > 0)
        {
            Dispatch(bitApplies, await kcpX.MonitorBitReadAsync(generation, bitApplies.Length));
        }
        if (wordEntries.Length > 0)
        {
            DispatchWords(await kcpX.MonitorWordReadAsync(generation, wordPointCount));
        }
    }

    private static void Dispatch<TValue>(Action<TValue>[] applies, TValue[] values)
    {
        for (int i = 0; i < applies.Length; i++)
        {
            applies[i](values[i]);
        }
    }

    private void DispatchWords(string[] tokens)
    {
        int offset = 0;
        foreach (var (points, apply) in wordEntries)
        {
            apply(tokens.Skip(offset).Take(points).ToArray());
            offset += points;
        }
    }
}
