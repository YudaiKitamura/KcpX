using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// 強制セット（ST）、強制リセット（RS）
/// </summary>
internal sealed class ForceSetResetCommand : IPlcCommand<bool>
{
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ForceSetResetCommand(Prefix prefix, string address, bool isSet)
    {
        bool isSupported = DeviceConverter.IsBitDevice(prefix)
            || prefix is Prefix.T or Prefix.C
            || (prefix == Prefix.CTC && !isSet);
        if (!isSupported)
        {
            throw new NotSupportedException($"Device {prefix} cannot be force {(isSet ? "set" : "reset")}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            isSet ? "ST" : "RS",
            DeviceConverter.ToDeviceText(prefix, address, DataFormat.Bit));
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
