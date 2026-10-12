using System.Text;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Utils;

namespace KcpXLib.Parsers;

internal static class ResponseParser
{
    private static readonly char[] separators = [' '];
    // T, C, CTH, CTC の「接点,現在値,設定値」はカンマと空白のどちらで区切られても解釈する。
    private static readonly char[] timerCounterSeparators = [' ', ','];

    /// <summary>
    /// 末尾の CR LF を除いたレスポンスを返します。エラーレスポンスの場合は <see cref="HostLinkException"/> をスローします。
    /// </summary>
    internal static string Parse(byte[] response)
    {
        var text = Encoding.ASCII.GetString(response);
        if (!text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            throw new ReceivePacketException("The response does not end with CR LF.");
        }
        text = text.Substring(0, text.Length - 2);

        if (text.Length == 2 && text[0] == 'E' && char.IsDigit(text[1]))
        {
            var code = (HostLinkErrorCode)(text[1] - '0');
            throw new HostLinkException($"The PLC returned an error response: {text}.", code);
        }
        return text;
    }

    internal static void ParseOk(byte[] response)
    {
        var text = Parse(response);
        if (text != "OK")
        {
            throw new ReceivePacketException($"Unexpected response: {text}.");
        }
    }

    internal static string[] ParseValues(byte[] response, int expectedCount)
    {
        var tokens = Parse(response).Split(separators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != expectedCount)
        {
            throw new ReceivePacketException($"Expected {expectedCount} values but received {tokens.Length}.");
        }
        return tokens;
    }

    internal static TimerCounterValue[] ParseTimerCounterValues(byte[] response, int expectedCount)
    {
        var tokens = Parse(response).Split(timerCounterSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != expectedCount * 3)
        {
            throw new ReceivePacketException($"Expected {expectedCount} timer/counter values but received {tokens.Length} fields.");
        }

        var values = new TimerCounterValue[expectedCount];
        for (int i = 0; i < expectedCount; i++)
        {
            var contact = tokens[i * 3] switch
            {
                "0" => false,
                "1" => true,
                _ => throw new ReceivePacketException($"Invalid contact value: {tokens[i * 3]}."),
            };
            values[i] = new TimerCounterValue(contact, ValueConverter.ParseUInt32(tokens[i * 3 + 1]), ValueConverter.ParseUInt32(tokens[i * 3 + 2]));
        }
        return values;
    }

    /// <summary>
    /// 読出しコマンドのレスポンスを型 <typeparamref name="T"/> の値に変換します。
    /// T, C, CTH, CTC を <c>bool</c> で読み出した場合は接点の状態を返します。
    /// </summary>
    internal static T[] ParseDeviceValues<T>(byte[] response, Prefix prefix, int length) where T : unmanaged
    {
        if (DeviceConverter.IsTimerCounterDevice(prefix))
        {
            return ParseTimerCounterValues(response, length).Select(v => (T)(object)v.Contact).ToArray();
        }
        return ValueConverter.FromTokens<T>(ParseValues(response, length * ValueConverter.GetPoints<T>()));
    }
}
