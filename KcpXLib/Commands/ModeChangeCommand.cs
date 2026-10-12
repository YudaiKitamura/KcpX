using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;

namespace KcpXLib.Commands;

/// <summary>
/// モード切替（M0: PROGRAM、M1: RUN）
/// </summary>
internal sealed class ModeChangeCommand : IPlcCommand<bool>
{
    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ModeChangeCommand(OperatingMode mode)
    {
        if (mode is not (OperatingMode.Program or OperatingMode.Run))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Specify OperatingMode.Program or OperatingMode.Run.");
        }

        commandPacketBuilder = new CommandPacketBuilder(mode == OperatingMode.Run ? "M1" : "M0");
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
