using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// モニタ登録（MWS、ワード）
/// </summary>
internal sealed class MonitorWordRegistCommand : IPlcCommand<bool>
{
    internal const int MIN_COUNT = 1;
    internal const int MAX_COUNT = 120;

    private readonly CommandPacketBuilder commandPacketBuilder;

    internal MonitorWordRegistCommand((Prefix Prefix, string Address, DataFormat Format)[] devices)
    {
        if (devices.Length is < MIN_COUNT or > MAX_COUNT)
        {
            throw new ArgumentOutOfRangeException(nameof(devices), $"The number of points must be {MIN_COUNT} to {MAX_COUNT}.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            "MWS",
            devices.Select(d =>
            {
                Validate(d.Prefix, d.Address);
                return DeviceConverter.ToDeviceText(d.Prefix, d.Address, d.Format);
            }).ToArray());
    }

    /// <summary>
    /// ワードモニタに登録できるデバイスか検証します。リレーはチャンネル単位（ビット番号 00）でのみ登録できます。
    /// </summary>
    internal static void Validate(Prefix prefix, string address)
    {
        if (prefix is Prefix.T or Prefix.C)
        {
            throw new NotSupportedException($"Device {prefix} cannot be registered for word monitoring. Use TC/TS or CC/CS instead.");
        }

        if (DeviceConverter.IsRelayDevice(prefix) && DeviceConverter.ToIndex(prefix, address) % 16 != 0)
        {
            throw new DeviceAddressException($"{prefix}{address} is invalid. Relays must be registered by channel (bit number 00).");
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
