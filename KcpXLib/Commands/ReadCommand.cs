using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// データ読出し（RD）
/// </summary>
internal sealed class ReadCommand<T> : IPlcCommand<T> where T : unmanaged
{
    private readonly Prefix prefix;
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ReadCommand(Prefix prefix, string address)
    {
        this.prefix = prefix;
        ReadValidator.Validate<T>(prefix);
        if (ValueConverter.GetPoints<T>() != 1)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} cannot be read with the RD command.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            "RD",
            DeviceConverter.ToDeviceText(prefix, address, ReadValidator.GetFormat<T>(prefix)));
    }

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public T Execute(IPlc plc)
    {
        return ResponseParser.ParseDeviceValues<T>(plc.Request(ToAsciiBytes()), prefix, 1)[0];
    }

    public async Task<T> ExecuteAsync(IPlc plc)
    {
        return ResponseParser.ParseDeviceValues<T>(await plc.RequestAsync(ToAsciiBytes()), prefix, 1)[0];
    }
}
