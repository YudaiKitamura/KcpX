using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;

namespace KcpXLib.Commands;

/// <summary>
/// 動作モード確認（?M）
/// </summary>
internal sealed class ModeReadCommand : IPlcCommand<OperatingMode>
{
    private readonly CommandPacketBuilder commandPacketBuilder = new("?M");

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public OperatingMode Execute(IPlc plc) => Parse(plc.Request(ToAsciiBytes()));

    public async Task<OperatingMode> ExecuteAsync(IPlc plc) => Parse(await plc.RequestAsync(ToAsciiBytes()));

    private static OperatingMode Parse(byte[] response) => ResponseParser.Parse(response) switch
    {
        "0" => OperatingMode.Program,
        "1" => OperatingMode.Run,
        var text => throw new ReceivePacketException($"Invalid operating mode: {text}."),
    };
}
