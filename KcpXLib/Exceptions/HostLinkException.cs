using KcpXLib.Enums;

namespace KcpXLib.Exceptions;

/// <summary>
/// 上位リンクの交信例外
/// </summary>
/// <remarks>
/// PLC からエラーレスポンス（E0〜E6）を受信した場合にスローします。
/// </remarks>
/// <param name="message">詳細の内容</param>
/// <param name="errorCode">PLC から受信したエラーコード</param>
public class HostLinkException(string message, HostLinkErrorCode errorCode) : Exception(message)
{
    /// <summary>
    /// PLC から受信したエラーコード
    /// </summary>
    public HostLinkErrorCode ErrorCode { get; } = errorCode;
}
