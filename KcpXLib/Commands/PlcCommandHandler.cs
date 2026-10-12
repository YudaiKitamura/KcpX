using KcpXLib.Interfaces;

namespace KcpXLib.Commands;

internal class PlcCommandHandler<T>
{
    public T Execute(IPlcCommand<T> command, IPlc plc)
    {
        return command.Execute(plc);
    }

    public async Task<T> ExecuteAsync(IPlcCommand<T> command, IPlc plc)
    {
        return await command.ExecuteAsync(plc);
    }
}
