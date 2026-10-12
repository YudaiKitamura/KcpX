using System.Globalization;
using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// タイマ・カウンタの連続データ読出し（RDS、接点・現在値・設定値）
/// </summary>
internal sealed class ContinuousReadTimerCounterCommand : IPlcCommand<TimerCounterValue[]>
{
    internal const ushort MIN_LENGTH = 1;

    private readonly ushort length;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ContinuousReadTimerCounterCommand(Prefix prefix, string address, ushort length, PlcSeries series)
    {
        this.length = length;
        if (!DeviceConverter.IsTimerCounterDevice(prefix))
        {
            throw new NotSupportedException($"Device {prefix} is not a timer or counter. Specify T, C, CTH or CTC.");
        }

        int maxLength = DeviceConverter.GetMaxPoints(prefix, DataFormat.D, series);
        if (length < MIN_LENGTH || length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"The number of points must be {MIN_LENGTH} to {maxLength}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            "RDS",
            DeviceConverter.ToDeviceText(prefix, address, DataFormat.D),
            length.ToString(CultureInfo.InvariantCulture));
    }

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public TimerCounterValue[] Execute(IPlc plc)
    {
        return ResponseParser.ParseTimerCounterValues(plc.Request(ToAsciiBytes()), length);
    }

    public async Task<TimerCounterValue[]> ExecuteAsync(IPlc plc)
    {
        return ResponseParser.ParseTimerCounterValues(await plc.RequestAsync(ToAsciiBytes()), length);
    }
}
