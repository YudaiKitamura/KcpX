using System.Globalization;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace KcpXLib.Utils;

internal static class DeviceConverter
{
    private const int BITS_PER_CHANNEL = 16;

    internal static bool IsBitDevice(Prefix prefix) =>
        prefix is Prefix.R or Prefix.B or Prefix.MR or Prefix.LR or Prefix.CR or Prefix.VB;

    internal static bool IsRelayDevice(Prefix prefix) =>
        prefix is Prefix.R or Prefix.MR or Prefix.LR or Prefix.CR;

    internal static bool IsHexDevice(Prefix prefix) =>
        prefix is Prefix.B or Prefix.VB or Prefix.W;

    internal static bool IsWordDevice(Prefix prefix) =>
        prefix is Prefix.DM or Prefix.EM or Prefix.FM or Prefix.ZF or Prefix.W or Prefix.TM or Prefix.CM or Prefix.VM;

    /// <summary>
    /// デバイス番号 1 つが 32 ビットの値 1 つに対応するデバイス（Z, AT, TC, TS, CC, CS）
    /// </summary>
    internal static bool IsElementDevice(Prefix prefix) =>
        prefix is Prefix.Z or Prefix.AT or Prefix.TC or Prefix.TS or Prefix.CC or Prefix.CS;

    /// <summary>
    /// 読出し時に「接点,現在値,設定値」を返すデバイス（T, C, CTH, CTC）
    /// </summary>
    internal static bool IsTimerCounterDevice(Prefix prefix) =>
        prefix is Prefix.T or Prefix.C or Prefix.CTH or Prefix.CTC;

    /// <summary>
    /// アドレスを通し番号に変換します。リレーはチャンネル × 16 + ビット番号です。
    /// </summary>
    internal static uint ToIndex(Prefix prefix, string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid.");
        }

        if (IsHexDevice(prefix))
        {
            if (!uint.TryParse(address, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex))
            {
                throw new DeviceAddressException($"{prefix}{address} is invalid.");
            }
            return hex;
        }

        if (!address.All(c => c is >= '0' and <= '9')
            || !uint.TryParse(address, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid.");
        }

        if (!IsRelayDevice(prefix))
        {
            return value;
        }

        uint bit = value % 100;
        if (bit >= BITS_PER_CHANNEL)
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid. The bit number must be 00 to 15.");
        }
        return value / 100 * BITS_PER_CHANNEL + bit;
    }

    internal static string ToAddress(Prefix prefix, uint index)
    {
        if (IsHexDevice(prefix))
        {
            return index.ToString("X", CultureInfo.InvariantCulture);
        }

        if (IsRelayDevice(prefix))
        {
            return (index / BITS_PER_CHANNEL).ToString(CultureInfo.InvariantCulture)
                + (index % BITS_PER_CHANNEL).ToString("D2", CultureInfo.InvariantCulture);
        }

        return index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// アドレスを <paramref name="offset"/> だけ進めます。リレーはビット番号 15 の次が次チャンネルの 00 になります。
    /// </summary>
    internal static string GetOffsetAddress(Prefix prefix, string address, int offset)
    {
        long index = ToIndex(prefix, address) + (long)offset;
        if (index < 0 || index > uint.MaxValue)
        {
            throw new DeviceAddressException($"{prefix}{address} + {offset} is out of range.");
        }
        return ToAddress(prefix, (uint)index);
    }

    internal static string ToDeviceText(Prefix prefix, string address, DataFormat format)
    {
        return prefix.ToString() + ToAddress(prefix, ToIndex(prefix, address)) + ToSuffix(format);
    }

    internal static string ToSuffix(DataFormat format) => format switch
    {
        DataFormat.Bit => "",
        _ => "." + format,
    };

    /// <summary>
    /// RDS / WRS で 1 回に指定できる点数の上限を返します。
    /// </summary>
    internal static ushort GetMaxPoints(Prefix prefix, DataFormat format, PlcSeries series)
    {
        bool isNano = series == PlcSeries.KVNano;
        bool isDouble = format is DataFormat.D or DataFormat.L;

        return prefix switch
        {
            Prefix.Z => 12,
            Prefix.T or Prefix.TC or Prefix.TS or Prefix.C or Prefix.CC or Prefix.CS => (ushort)(isNano ? 64 : 120),
            Prefix.AT => 8,
            Prefix.CTH => (ushort)(isNano ? 4 : 2),
            Prefix.CTC => (ushort)(isNano ? 8 : 4),
            Prefix.TM when !isNano => (ushort)(isDouble ? 256 : 512),
            _ when isNano => (ushort)(isDouble ? 128 : 256),
            _ => (ushort)(isDouble ? 500 : 1000),
        };
    }
}
