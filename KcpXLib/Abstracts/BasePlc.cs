using KcpXLib.Interfaces;

namespace KcpXLib.Abstracts;

/// <summary>
/// PLC 通信抽象クラス
/// </summary>
public abstract class BasePlc : IDisposable
{
    private readonly IPlcTransport transport;

    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <param name="transport">通信トランスポートを指定します。</param>
    protected BasePlc(IPlcTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>
    /// リクエスト送信
    /// </summary>
    /// <param name="packet">PLC に送信するコマンドを指定します。</param>
    /// <returns>PLC から受信したレスポンスを返します。</returns>
    public byte[] Request(byte[] packet)
    {
        return transport.Request(packet);
    }

    /// <summary>
    /// リクエスト送信（非同期）
    /// </summary>
    /// <param name="packet">PLC に送信するコマンドを指定します。</param>
    /// <returns>PLC から受信したレスポンスを返します。</returns>
    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        return await transport.RequestAsync(packet);
    }

    /// <summary>
    /// インスタンス破棄
    /// </summary>
    /// <remarks>
    /// 使用済みのリソースを解放します。
    /// </remarks>
    public virtual void Dispose()
    {
        transport.Dispose();
    }
}
