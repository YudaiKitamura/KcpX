using KcpXLib.Builders;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// モニタ読出し（MBR、ビット）
/// </summary>
internal sealed class MonitorBitReadCommand(int count) : IPlcCommand<bool[]>
{
    private readonly CommandPacketBuilder commandPacketBuilder = new("MBR");

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public bool[] Execute(IPlc plc)
    {
        return ValueConverter.FromTokens<bool>(ResponseParser.ParseValues(plc.Request(ToAsciiBytes()), count));
    }

    public async Task<bool[]> ExecuteAsync(IPlc plc)
    {
        return ValueConverter.FromTokens<bool>(ResponseParser.ParseValues(await plc.RequestAsync(ToAsciiBytes()), count));
    }
}
