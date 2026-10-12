using System.Globalization;
using KcpXLib.Builders;
using KcpXLib.Enums;
using KcpXLib.Interfaces;
using KcpXLib.Parsers;
using KcpXLib.Utils;

namespace KcpXLib.Commands;

/// <summary>
/// 連続データ書込み（WRS）、連続設定値書込み（WSS）
/// </summary>
internal sealed class ContinuousWriteCommand<T> : IPlcCommand<bool> where T : unmanaged
{
    internal const int MIN_LENGTH = 1;

    private readonly CommandPacketBuilder commandPacketBuilder;

    internal ContinuousWriteCommand(Prefix prefix, string address, T[] values, PlcSeries series, bool isSetValue = false)
    {
        WriteValidator.Validate<T>(prefix);
        if (isSetValue)
        {
            WriteValidator.ValidateSetValue(prefix);
        }

        var format = ValueConverter.GetFormat<T>();
        var tokens = ValueConverter.ToTokens(values);
        int maxPoints = DeviceConverter.GetMaxPoints(prefix, format, series);
        if (values.Length < MIN_LENGTH || tokens.Length > maxPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(values), $"The number of points must be {MIN_LENGTH} to {maxPoints}.");
        }

        var parameters = new List<string>
        {
            DeviceConverter.ToDeviceText(prefix, address, format),
            tokens.Length.ToString(CultureInfo.InvariantCulture),
        };
        parameters.AddRange(tokens);
        commandPacketBuilder = new CommandPacketBuilder(isSetValue ? "WSS" : "WRS", parameters.ToArray());
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
