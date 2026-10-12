using System.Globalization;
using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// 連続強制セット（STS）、連続強制リセット（RSS）
/// </summary>
internal sealed class ContinuousForceSetResetCommand : IPlcCommand<bool>
{
    internal const byte MIN_COUNT = 1;
    internal const byte MAX_COUNT = 16;

    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ContinuousForceSetResetCommand(Prefix prefix, string address, byte count, bool isSet)
    {
        if (!DeviceConverter.IsBitDevice(prefix))
        {
            throw new NotSupportedException($"Device {prefix} cannot be force {(isSet ? "set" : "reset")} continuously.");
        }
        if (count is < MIN_COUNT or > MAX_COUNT)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"The count must be {MIN_COUNT} to {MAX_COUNT}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            isSet ? "STS" : "RSS",
            DeviceConverter.ToDeviceText(prefix, address, DataFormat.Bit),
            count.ToString(CultureInfo.InvariantCulture));
    }

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public bool Execute(IPlc plc)
    {
        ResponseParser.ParseOk(plc.Request(ToAsciiBytes()));
        return true;
    }

    public async Task<bool> ExecuteAsync(IPlc plc)
    {
        ResponseParser.ParseOk(await plc.RequestAsync(ToAsciiBytes()));
        return true;
    }
}
