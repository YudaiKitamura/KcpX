using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// モニタ登録（MBS、ビット）
/// </summary>
internal sealed class MonitorBitRegistCommand : IPlcCommand<bool>
{
    internal const int MIN_COUNT = 1;
    internal const int MAX_COUNT = 120;

    private readonly CommandPacketBuilder commandPacketBuilder;

    internal MonitorBitRegistCommand((Prefix Prefix, string Address)[] devices)
    {
        if (devices.Length is < MIN_COUNT or > MAX_COUNT)
        {
            throw new ArgumentOutOfRangeException(nameof(devices), $"The number of devices must be {MIN_COUNT} to {MAX_COUNT}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            "MBS",
            devices.Select(d =>
            {
                Validate(d.Prefix);
                return DeviceConverter.ToDeviceText(d.Prefix, d.Address, DataFormat.Bit);
            }).ToArray());
    }

    internal static void Validate(Prefix prefix)
    {
        if (!DeviceConverter.IsBitDevice(prefix) && prefix is not (Prefix.T or Prefix.C or Prefix.CTC))
        {
            throw new NotSupportedException($"Device {prefix} cannot be registered for bit monitoring.");
        }
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
