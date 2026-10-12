namespace KcpXLib.Exceptions;

/// <summary>
/// デバイスアドレス例外
/// </summary>
/// <remarks>
/// 指定したデバイスのアドレスの書式が不正な場合にスローします。
/// </remarks>
/// <param name="message">詳細の内容</param>
public class DeviceAddressException(string message) : Exception(message)
{
}
