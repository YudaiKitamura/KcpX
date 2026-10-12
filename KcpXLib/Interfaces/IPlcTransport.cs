namespace KcpXLib.Interfaces;

/// <summary>
/// PLC との通信トランスポート
/// </summary>
public interface IPlcTransport : IDisposable
{
    /// <summary>
    /// パケットを送信し、CR LF までのレスポンスを返します。
    /// </summary>
    byte[] Request(byte[] packet);

    /// <summary>
    /// パケットを非同期で送信し、CR LF までのレスポンスを返します。
    /// </summary>
    Task<byte[]> RequestAsync(byte[] packet);
}
