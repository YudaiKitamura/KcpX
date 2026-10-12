using KcpXLib.Interfaces;

namespace TestKcpX;

/// <summary>
/// <see cref="FakeKvCore"/> にコマンドを渡すテスト用トランスポート。
/// </summary>
internal sealed class FakePlcTransport(FakeKvCore core) : IPlcTransport
{
    internal FakeKvCore Core { get; } = core;

    public FakePlcTransport() : this(new FakeKvCore())
    {
    }

    public byte[] Request(byte[] packet) => Core.Handle(packet);

    public Task<byte[]> RequestAsync(byte[] packet) => Task.FromResult(Core.Handle(packet));

    public void Dispose()
    {
    }
}
