using KcpXLib.Enums;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

internal static class WriteValidator
{
    /// <summary>
    /// 書込みできるデバイスと型の組み合わせか検証します。T, C, CTH, CTC は数値（現在値または設定値）のみ書込みできます。
    /// </summary>
    internal static void Validate<T>(Prefix prefix) where T : unmanaged
    {
        ValueConverter.Validate<T>(prefix);
        if (DeviceConverter.IsTimerCounterDevice(prefix) && typeof(T) == typeof(bool))
        {
            throw new NotSupportedException($"Device {prefix} cannot be written as bool. Use ForceSet or ForceReset for the contact.");
        }
    }

    /// <summary>
    /// 設定値書込み（WS / WSS）できるデバイスか検証します。
    /// </summary>
    internal static void ValidateSetValue(Prefix prefix)
    {
        if (prefix is not (Prefix.T or Prefix.C or Prefix.CTC))
        {
            throw new NotSupportedException($"Device {prefix} has no set value. Specify T, C or CTC.");
        }
    }
}
