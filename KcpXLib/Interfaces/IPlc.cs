using KcpXLib.Enums;

namespace KcpXLib.Interfaces;

/// <summary>
/// コマンドの送信先 PLC
/// </summary>
public interface IPlc
{
    /// <summary>
    /// 接続先 PLC のシリーズ
    /// </summary>
    PlcSeries Series { get; }

    /// <summary>
    /// コマンドを送信し、レスポンス（末尾 CR LF を含む）を返します。
    /// </summary>
    byte[] Request(byte[] packet);

    /// <summary>
    /// コマンドを非同期で送信し、レスポンス（末尾 CR LF を含む）を返します。
    /// </summary>
    Task<byte[]> RequestAsync(byte[] packet);
}
