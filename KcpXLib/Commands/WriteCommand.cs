using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// データ書込み（WR）、設定値書込み（WS）
/// </summary>
internal sealed class WriteCommand<T> : IPlcCommand<bool> where T : unmanaged
{
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal WriteCommand(Prefix prefix, string address, T value, bool isSetValue = false)
    {
        string command = isSetValue ? "WS" : "WR";
        WriteValidator.Validate<T>(prefix);
        if (isSetValue)
        {
            WriteValidator.ValidateSetValue(prefix);
        }
        if (ValueConverter.GetPoints<T>() != 1)
        {
            throw new NotSupportedException($"Type {typeof(T).Name} cannot be written with the {command} command.");
        }

        commandPacketBuilder = new CommandPacketBuilder(
            command,
            DeviceConverter.ToDeviceText(prefix, address, ValueConverter.GetFormat<T>()),
            ValueConverter.ToTokens([value])[0]);
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
