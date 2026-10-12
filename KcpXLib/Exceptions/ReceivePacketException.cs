namespace KcpXLib.Exceptions;

/// <summary>
/// 受信パケット例外
/// </summary>
/// <remarks>
/// PLC から受信したレスポンスの内容が不正な場合にスローします。
/// </remarks>
/// <param name="message">詳細の内容</param>
public class ReceivePacketException(string message) : Exception(message)
{
}
