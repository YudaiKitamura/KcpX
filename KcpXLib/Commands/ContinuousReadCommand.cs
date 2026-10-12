using System.Globalization;
using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// 連続データ読出し（RDS）
/// </summary>
internal sealed class ContinuousReadCommand<T> : IPlcCommand<T[]> where T : unmanaged
{
    internal const ushort MIN_LENGTH = 1;

    private readonly Prefix prefix;
    private readonly ushort length;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ContinuousReadCommand(Prefix prefix, string address, ushort length, PlcSeries series)
    {
        this.prefix = prefix;
        this.length = length;
        ReadValidator.Validate<T>(prefix);

        var format = ReadValidator.GetFormat<T>(prefix);
        int points = length * ValueConverter.GetPoints<T>();
        int maxPoints = DeviceConverter.GetMaxPoints(prefix, format, series);
        if (length < MIN_LENGTH || points > maxPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"The number of points must be {MIN_LENGTH} to {maxPoints}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            "RDS",
            DeviceConverter.ToDeviceText(prefix, address, format),
            points.ToString(CultureInfo.InvariantCulture));
    }

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public T[] Execute(IPlc plc)
    {
        return ResponseParser.ParseDeviceValues<T>(plc.Request(ToAsciiBytes()), prefix, length);
    }

    public async Task<T[]> ExecuteAsync(IPlc plc)
    {
        return ResponseParser.ParseDeviceValues<T>(await plc.RequestAsync(ToAsciiBytes()), prefix, length);
    }
}
