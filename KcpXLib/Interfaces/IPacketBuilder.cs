namespace KcpXLib.Interfaces;

/// <summary>
/// 送信パケットのビルダー
/// </summary>
public interface IPacketBuilder
{
    /// <summary>
    /// 送信するコマンドを ASCII のバイト列（末尾 CR）で返します。
    /// </summary>
    byte[] ToAsciiBytes();
}
