using System.Globalization;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace KcpXLib.Utils;

internal static class ValueConverter
{
    /// <summary>
    /// 型 <typeparamref name="T"/> を読み書きするデータ形式を返します。64 ビットの型は .D を 2 点使用します。
    /// </summary>
    internal static DataFormat GetFormat<T>() where T : unmanaged
    {
        var type = typeof(T);
        if (type == typeof(bool)) return DataFormat.Bit;
        if (type == typeof(ushort)) return DataFormat.U;
        if (type == typeof(short)) return DataFormat.S;
        if (type == typeof(int)) return DataFormat.L;
        if (type == typeof(uint) || type == typeof(float)) return DataFormat.D;
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)) return DataFormat.D;

        throw new NotSupportedException($"Type {type.Name} is not supported.");
    }

    /// <summary>
    /// 型 <typeparamref name="T"/> の 1 要素あたりのコマンド上の点数を返します。
    /// </summary>
    internal static int GetPoints<T>() where T : unmanaged => GetWordLength<T>() == 4 ? 2 : 1;

    internal static int GetWordLength<T>() where T : unmanaged
    {
        var type = typeof(T);
        if (type == typeof(bool) || type == typeof(ushort) || type == typeof(short)) return 1;
        if (type == typeof(int) || type == typeof(uint) || type == typeof(float)) return 2;
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)) return 4;

        throw new NotSupportedException($"Type {type.Name} is not supported.");
    }

    /// <summary>
    /// 1 要素あたりに進むアドレス数を返します。ビットデバイスはビット単位、それ以外はデバイス番号単位です。
    /// </summary>
    internal static int GetStride<T>(Prefix prefix) where T : unmanaged
    {
        if (DeviceConverter.IsElementDevice(prefix) || DeviceConverter.IsTimerCounterDevice(prefix))
        {
            return 1;
        }
        if (DeviceConverter.IsBitDevice(prefix))
        {
            return typeof(T) == typeof(bool) ? 1 : GetWordLength<T>() * 16;
        }
        return GetWordLength<T>();
    }

    /// <summary>
    /// デバイスと型の組み合わせが読み書きできるか検証します。
    /// </summary>
    internal static void Validate<T>(Prefix prefix) where T : unmanaged
    {
        GetFormat<T>();
        var type = typeof(T);

        if (type == typeof(bool))
        {
            if (!DeviceConverter.IsBitDevice(prefix) && !DeviceConverter.IsTimerCounterDevice(prefix))
            {
                throw new NotSupportedException($"Device {prefix} cannot be accessed as bool.");
            }
            return;
        }

        if ((DeviceConverter.IsElementDevice(prefix) || DeviceConverter.IsTimerCounterDevice(prefix))
            && (GetWordLength<T>() == 4 || type == typeof(float)))
        {
            throw new NotSupportedException($"Device {prefix} cannot be accessed as {type.Name}.");
        }
    }

    internal static string[] ToTokens<T>(IEnumerable<T> values) where T : unmanaged
    {
        var tokens = new List<string>();
        foreach (var value in values)
        {
            switch (value)
            {
                case bool b: tokens.Add(b ? "1" : "0"); break;
                case ushort u: tokens.Add(u.ToString(CultureInfo.InvariantCulture)); break;
                case short s: tokens.Add(s.ToString(CultureInfo.InvariantCulture)); break;
                case uint u: tokens.Add(u.ToString(CultureInfo.InvariantCulture)); break;
                case int i: tokens.Add(i.ToString(CultureInfo.InvariantCulture)); break;
                case float f: tokens.Add(BitConverter.ToUInt32(BitConverter.GetBytes(f), 0).ToString(CultureInfo.InvariantCulture)); break;
                case long l: AddUInt64(tokens, unchecked((ulong)l)); break;
                case ulong ul: AddUInt64(tokens, ul); break;
                case double d: AddUInt64(tokens, unchecked((ulong)BitConverter.DoubleToInt64Bits(d))); break;
                default: throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
            }
        }
        return tokens.ToArray();
    }

    // 下位 32 ビットを先頭のデバイスに格納する。
    private static void AddUInt64(List<string> tokens, ulong value)
    {
        tokens.Add(((uint)(value & 0xFFFFFFFF)).ToString(CultureInfo.InvariantCulture));
        tokens.Add(((uint)(value >> 32)).ToString(CultureInfo.InvariantCulture));
    }

    internal static T[] FromTokens<T>(string[] tokens) where T : unmanaged
    {
        int points = GetPoints<T>();
        if (tokens.Length % points != 0)
        {
            throw new ReceivePacketException($"Unexpected number of values: {tokens.Length}.");
        }

        var values = new T[tokens.Length / points];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = points == 1
                ? FromToken<T>(tokens[i])
                : FromUInt64<T>(ParseUInt32(tokens[i * 2]) | (ulong)ParseUInt32(tokens[i * 2 + 1]) << 32);
        }
        return values;
    }

    private static T FromToken<T>(string token) where T : unmanaged
    {
        var type = typeof(T);
        object value;
        try
        {
            if (type == typeof(bool))
            {
                value = token switch
                {
                    "0" => false,
                    "1" => true,
                    _ => throw new FormatException(),
                };
            }
            else if (type == typeof(ushort)) value = ushort.Parse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            else if (type == typeof(short)) value = short.Parse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            else if (type == typeof(uint)) value = ParseUInt32(token);
            else if (type == typeof(int)) value = int.Parse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            else if (type == typeof(float)) value = BitConverter.ToSingle(BitConverter.GetBytes(ParseUInt32(token)), 0);
            else throw new NotSupportedException($"Type {type.Name} is not supported.");
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ReceivePacketException($"Invalid value: {token}.");
        }
        return (T)value;
    }

    private static T FromUInt64<T>(ulong value) where T : unmanaged
    {
        var type = typeof(T);
        object result;
        if (type == typeof(ulong)) result = value;
        else if (type == typeof(long)) result = unchecked((long)value);
        else if (type == typeof(double)) result = BitConverter.Int64BitsToDouble(unchecked((long)value));
        else throw new NotSupportedException($"Type {type.Name} is not supported.");
        return (T)result;
    }

    internal static uint ParseUInt32(string token)
    {
        if (!uint.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
        {
            throw new ReceivePacketException($"Invalid value: {token}.");
        }
        return value;
    }
}
