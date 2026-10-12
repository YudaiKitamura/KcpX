using KcpXLib.Enums;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

internal static class ReadValidator
{
    /// <summary>
    /// 読出しできるデバイスと型の組み合わせか検証します。T, C, CTH, CTC は <c>bool</c>（接点）のみ読出しできます。
    /// </summary>
    internal static void Validate<T>(Prefix prefix) where T : unmanaged
    {
        ValueConverter.Validate<T>(prefix);
        if (DeviceConverter.IsTimerCounterDevice(prefix) && typeof(T) != typeof(bool))
        {
            throw new NotSupportedException(
                $"Device {prefix} can only be read as bool (contact). Use ReadTimerCounter, or TC/TS/CC/CS for the current and set values.");
        }
    }

    internal static DataFormat GetFormat<T>(Prefix prefix) where T : unmanaged
    {
        // T, C, CTH, CTC は形式を .D にすると「接点,現在値,設定値」で返る。
        return DeviceConverter.IsTimerCounterDevice(prefix) ? DataFormat.D : ValueConverter.GetFormat<T>();
    }
}
