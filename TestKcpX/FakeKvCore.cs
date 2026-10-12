using System.Globalization;
using System.Text;
using KcpXLib.Enums;
using KcpXLib.Utils;

namespace TestKcpX;

/// <summary>
/// 上位リンクのコマンドを解釈し、メモリ上のデバイスに読み書きするダミー PLC。
/// </summary>
internal sealed class FakeKvCore
{
    // 最長一致で判定するため、長い名前から並べる。
    private static readonly Prefix[] prefixesByNameLength =
        ((Prefix[])Enum.GetValues(typeof(Prefix))).OrderByDescending(d => d.ToString().Length).ToArray();

    /// <summary>ビットデバイスと T, C, CTC の接点（キー: デバイス, 通し番号）</summary>
    internal Dictionary<(Prefix, uint), bool> Bits { get; } = new();

    /// <summary>ワードデバイス（キー: デバイス, 番号）</summary>
    internal Dictionary<(Prefix, uint), ushort> Words { get; } = new();

    /// <summary>Z, AT, TC, TS, CC, CS, CTH, CTC の 32 ビット値（キー: デバイス, 番号）</summary>
    internal Dictionary<(Prefix, uint), uint> Elements { get; } = new();

    /// <summary>受信したコマンド（末尾 CR を除く）</summary>
    internal List<string> Requests { get; } = new();

    /// <summary>設定するとすべてのコマンドにこのエラーレスポンスを返す。</summary>
    internal HostLinkErrorCode? ErrorCode { get; set; }

    /// <summary>動作モード（?M / M0 / M1）</summary>
    internal OperatingMode Mode { get; set; } = OperatingMode.Run;

    private List<(Prefix, uint)>? bitMonitor;
    private List<(Prefix Prefix, uint Index, DataFormat Format)>? wordMonitor;

    internal byte[] Handle(byte[] packet)
    {
        var command = Encoding.ASCII.GetString(packet).TrimEnd('\r');
        Requests.Add(command);
        string response;
        try
        {
            response = ErrorCode is { } code ? code.ToString() : Execute(command.Split(' '));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or IndexOutOfRangeException or KeyNotFoundException)
        {
            response = "E1";
        }
        return Encoding.ASCII.GetBytes(response + "\r\n");
    }

    private string Execute(string[] args)
    {
        switch (args[0])
        {
            case "RD":
            {
                var (prefix, index, format) = ParseDevice(args[1]);
                return ReadText(prefix, index, format);
            }
            case "RDS":
            {
                var (prefix, index, format) = ParseDevice(args[1]);
                int count = int.Parse(args[2], CultureInfo.InvariantCulture);
                return string.Join(" ", Enumerable.Range(0, count)
                    .Select(i => ReadText(prefix, (uint)(index + i * Stride(prefix, format)), format)));
            }
            case "WR":
            {
                var (prefix, index, format) = ParseDevice(args[1]);
                WriteValue(prefix, index, format, args[2]);
                return "OK";
            }
            case "WRS":
            {
                var (prefix, index, format) = ParseDevice(args[1]);
                int count = int.Parse(args[2], CultureInfo.InvariantCulture);
                if (args.Length != count + 3)
                {
                    return "E1";
                }
                for (int i = 0; i < count; i++)
                {
                    WriteValue(prefix, (uint)(index + i * Stride(prefix, format)), format, args[i + 3]);
                }
                return "OK";
            }
            case "WS":
            case "WSS":
            {
                var (prefix, index, format) = ParseDevice(args[1]);
                if (prefix is not (Prefix.T or Prefix.C or Prefix.CTC))
                {
                    return "E1";
                }
                var (_, set) = GetTimerCounterStorage(prefix);
                var values = args[0] == "WS" ? new[] { args[2] } : args.Skip(3).ToArray();
                for (int i = 0; i < values.Length; i++)
                {
                    SetRaw(set, index + (uint)i, format, ParseValue(values[i], format));
                }
                return "OK";
            }
            case "ST":
            case "RS":
            {
                var (prefix, index, _) = ParseDevice(args[1]);
                Bits[(prefix, index)] = args[0] == "ST";
                return "OK";
            }
            case "STS":
            case "RSS":
            {
                var (prefix, index, _) = ParseDevice(args[1]);
                int count = int.Parse(args[2], CultureInfo.InvariantCulture);
                for (int i = 0; i < count; i++)
                {
                    Bits[(prefix, index + (uint)i)] = args[0] == "STS";
                }
                return "OK";
            }
            case "MBS":
                bitMonitor = args.Skip(1).Select(a =>
                {
                    var (prefix, index, _) = ParseDevice(a);
                    return (prefix, index);
                }).ToList();
                return "OK";
            case "MBR":
                if (bitMonitor == null)
                {
                    return "E0";
                }
                return string.Join(" ", bitMonitor.Select(m => Bits.GetValueOrDefault(m) ? "1" : "0"));
            case "MWS":
                wordMonitor = args.Skip(1).Select(ParseDevice).ToList();
                return "OK";
            case "MWR":
                if (wordMonitor == null)
                {
                    return "E0";
                }
                return string.Join(" ", wordMonitor.Select(m => Format(ReadMonitorWord(m.Prefix, m.Index, m.Format), m.Format)));
            case "?M" when args.Length == 1:
                return ((int)Mode).ToString(CultureInfo.InvariantCulture);
            case "M0" or "M1" when args.Length == 1:
                Mode = args[0] == "M1" ? OperatingMode.Run : OperatingMode.Program;
                return "OK";
            default:
                return "E1";
        }
    }

    internal static (Prefix Prefix, uint Index, DataFormat Format) ParseDevice(string text)
    {
        var prefix = prefixesByNameLength.First(d => text.StartsWith(d.ToString(), StringComparison.Ordinal));
        var rest = text.Substring(prefix.ToString().Length);
        var format = DefaultFormat(prefix);
        int dot = rest.IndexOf('.');
        if (dot >= 0)
        {
            format = (DataFormat)Enum.Parse(typeof(DataFormat), rest.Substring(dot + 1));
            rest = rest.Substring(0, dot);
        }
        return (prefix, DeviceConverter.ToIndex(prefix, rest), format);
    }

    private static DataFormat DefaultFormat(Prefix prefix)
    {
        if (DeviceConverter.IsBitDevice(prefix)) return DataFormat.Bit;
        if (DeviceConverter.IsWordDevice(prefix) || prefix == Prefix.Z) return DataFormat.U;
        return DataFormat.D;
    }

    internal static int Stride(Prefix prefix, DataFormat format)
    {
        bool isDouble = format is DataFormat.D or DataFormat.L;
        if (DeviceConverter.IsBitDevice(prefix))
        {
            return format == DataFormat.Bit ? 1 : isDouble ? 32 : 16;
        }
        if (DeviceConverter.IsWordDevice(prefix))
        {
            return isDouble ? 2 : 1;
        }
        return 1;
    }

    /// <summary>
    /// デバイスの値を、指定した形式のビット幅で返す。
    /// </summary>
    internal uint GetRaw(Prefix prefix, uint index, DataFormat format)
    {
        bool isDouble = format is DataFormat.D or DataFormat.L;
        if (DeviceConverter.IsBitDevice(prefix))
        {
            if (format == DataFormat.Bit)
            {
                return Bits.GetValueOrDefault((prefix, index)) ? 1u : 0u;
            }
            uint value = 0;
            for (int bit = 0; bit < (isDouble ? 32 : 16); bit++)
            {
                if (Bits.GetValueOrDefault((prefix, index + (uint)bit)))
                {
                    value |= 1u << bit;
                }
            }
            return value;
        }
        if (DeviceConverter.IsWordDevice(prefix))
        {
            uint low = Words.GetValueOrDefault((prefix, index));
            return isDouble ? low | (uint)Words.GetValueOrDefault((prefix, index + 1)) << 16 : low;
        }
        uint element = Elements.GetValueOrDefault((prefix, index));
        return isDouble ? element : element & 0xFFFF;
    }

    internal void SetRaw(Prefix prefix, uint index, DataFormat format, uint value)
    {
        bool isDouble = format is DataFormat.D or DataFormat.L;
        if (DeviceConverter.IsBitDevice(prefix))
        {
            int bits = format == DataFormat.Bit ? 1 : isDouble ? 32 : 16;
            for (int bit = 0; bit < bits; bit++)
            {
                Bits[(prefix, index + (uint)bit)] = (value >> bit & 1) != 0;
            }
            return;
        }
        if (DeviceConverter.IsWordDevice(prefix))
        {
            Words[(prefix, index)] = (ushort)value;
            if (isDouble)
            {
                Words[(prefix, index + 1)] = (ushort)(value >> 16);
            }
            return;
        }
        Elements[(prefix, index)] = isDouble ? value : (Elements.GetValueOrDefault((prefix, index)) & 0xFFFF0000) | (value & 0xFFFF);
    }

    /// <summary>
    /// T, C の現在値・設定値は TC/TS, CC/CS に格納する。CTH は現在値のみ、CTC は設定値のみ。
    /// </summary>
    internal static (Prefix Current, Prefix Set) GetTimerCounterStorage(Prefix prefix) => prefix switch
    {
        Prefix.T => (Prefix.TC, Prefix.TS),
        Prefix.C => (Prefix.CC, Prefix.CS),
        _ => (prefix, prefix),
    };

    /// <summary>
    /// T, C, CTH は現在値、CTC は設定値に書き込む。
    /// </summary>
    private void WriteValue(Prefix prefix, uint index, DataFormat format, string text)
    {
        var value = ParseValue(text, format);
        if (DeviceConverter.IsTimerCounterDevice(prefix))
        {
            var (current, set) = GetTimerCounterStorage(prefix);
            SetRaw(prefix == Prefix.CTC ? set : current, index, format, value);
            return;
        }
        SetRaw(prefix, index, format, value);
    }

    private string ReadText(Prefix prefix, uint index, DataFormat format)
    {
        if (DeviceConverter.IsTimerCounterDevice(prefix))
        {
            var (current, set) = GetTimerCounterStorage(prefix);
            uint currentValue = prefix == Prefix.CTC ? 0 : GetRaw(current, index, format);
            uint setValue = prefix == Prefix.CTH ? 0 : GetRaw(set, index, format);
            bool contact = prefix != Prefix.CTH && Bits.GetValueOrDefault((prefix, index));
            return $"{(contact ? 1 : 0)},{Format(currentValue, format)},{Format(setValue, format)}";
        }
        return Format(GetRaw(prefix, index, format), format);
    }

    /// <summary>
    /// ワードモニタでは CTH は現在値、CTC は設定値のみを返す。
    /// </summary>
    private uint ReadMonitorWord(Prefix prefix, uint index, DataFormat format) => prefix switch
    {
        Prefix.CTH => GetRaw(GetTimerCounterStorage(prefix).Current, index, format),
        Prefix.CTC => GetRaw(GetTimerCounterStorage(prefix).Set, index, format),
        _ => GetRaw(prefix, index, format),
    };

    internal static string Format(uint value, DataFormat format) => format switch
    {
        DataFormat.Bit => value.ToString(CultureInfo.InvariantCulture),
        DataFormat.U => value.ToString("D5", CultureInfo.InvariantCulture),
        DataFormat.S => ((short)value).ToString("+00000;-00000", CultureInfo.InvariantCulture),
        DataFormat.D => value.ToString("D10", CultureInfo.InvariantCulture),
        DataFormat.L => ((int)value).ToString("+0000000000;-0000000000", CultureInfo.InvariantCulture),
        DataFormat.H => value.ToString("X4", CultureInfo.InvariantCulture),
        _ => throw new ArgumentException(null, nameof(format)),
    };

    internal static uint ParseValue(string text, DataFormat format) => format switch
    {
        DataFormat.H => uint.Parse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
        DataFormat.S or DataFormat.L => unchecked((uint)int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)),
        _ => uint.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
    };
}
