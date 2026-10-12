using System.Text;
using KcpXLib.Interfaces;

namespace KcpXLib.Builders;

internal sealed class CommandPacketBuilder(string command, params string[] parameters) : IPacketBuilder
{
    internal const byte CR = 0x0D;

    public byte[] ToAsciiBytes()
    {
        var text = new StringBuilder(command);
        foreach (var parameter in parameters)
        {
            text.Append(' ').Append(parameter);
        }
        text.Append((char)CR);

        return Encoding.ASCII.GetBytes(text.ToString());
    }
}
