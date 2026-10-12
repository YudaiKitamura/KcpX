using KcpXLib.Builders;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;

namespace KcpXLib.Commands;

/// <summary>
/// モニタ読出し（MWR、ワード）
/// </summary>
/// <remarks>
/// 登録時の形式で値を返すため、型への変換は呼出し側で登録内容ごとに行います。
/// </remarks>
internal sealed class MonitorWordReadCommand(int count) : IPlcCommand<string[]>
{
    private readonly CommandPacketBuilder commandPacketBuilder = new("MWR");

    public byte[] ToAsciiBytes() => commandPacketBuilder.ToAsciiBytes();

    public string[] Execute(IPlc plc)
    {
        return ResponseParser.ParseValues(plc.Request(ToAsciiBytes()), count);
    }

    public async Task<string[]> ExecuteAsync(IPlc plc)
    {
        return ResponseParser.ParseValues(await plc.RequestAsync(ToAsciiBytes()), count);
    }
}
