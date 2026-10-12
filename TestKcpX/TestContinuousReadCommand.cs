using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using Moq;

namespace TestKcpX;

[TestClass]
public sealed class TestContinuousReadCommand
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    private static IPlc MockPlc(string response)
    {
        var plc = new Mock<IPlc>();
        plc.Setup(p => p.Request(It.IsAny<byte[]>())).Returns(Bytes(response));
        plc.Setup(p => p.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(Bytes(response));
        return plc.Object;
    }

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("RDS R100 4\r"), new ContinuousReadCommand<bool>(Prefix.R, "100", 4, PlcSeries.KV8000).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RDS DM200.S 3\r"), new ContinuousReadCommand<short>(Prefix.DM, "200", 3, PlcSeries.KV8000).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RDS EM0.D 4\r"), new ContinuousReadCommand<double>(Prefix.EM, "0", 2, PlcSeries.KV8000).ToAsciiBytes());
    }

    [TestMethod]
    public async Task TestExecute()
    {
        CollectionAssert.AreEqual(
            new[] { true, false, true, false },
            new ContinuousReadCommand<bool>(Prefix.R, "100", 4, PlcSeries.KV8000).Execute(MockPlc("1 0 1 0\r\n")));
        CollectionAssert.AreEqual(
            new short[] { 15025, -25400, 0 },
            await new ContinuousReadCommand<short>(Prefix.DM, "200", 3, PlcSeries.KV8000).ExecuteAsync(MockPlc("+15025 -25400 +00000\r\n")));
        CollectionAssert.AreEqual(
            new[] { 1UL << 32 },
            new ContinuousReadCommand<ulong>(Prefix.DM, "0", 1, PlcSeries.KV8000).Execute(MockPlc("0000000000 0000000001\r\n")));
    }

    [TestMethod]
    public void TestExecuteCountMismatch()
    {
        Assert.ThrowsException<ReceivePacketException>(
            () => new ContinuousReadCommand<short>(Prefix.DM, "0", 3, PlcSeries.KV8000).Execute(MockPlc("+00001 +00002\r\n")));
    }

    [TestMethod]
    public void TestLength()
    {
        _ = new ContinuousReadCommand<ushort>(Prefix.DM, "0", 1000, PlcSeries.KV8000);
        _ = new ContinuousReadCommand<uint>(Prefix.DM, "0", 500, PlcSeries.KV8000);
        _ = new ContinuousReadCommand<long>(Prefix.DM, "0", 250, PlcSeries.KV8000);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadCommand<ushort>(Prefix.DM, "0", 0, PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadCommand<ushort>(Prefix.DM, "0", 1001, PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadCommand<uint>(Prefix.DM, "0", 501, PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadCommand<long>(Prefix.DM, "0", 251, PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadCommand<ushort>(Prefix.DM, "0", 257, PlcSeries.KVNano));
    }

    [TestMethod]
    public void TestTimerCounter()
    {
        var command = new ContinuousReadTimerCounterCommand(Prefix.T, "0", 2, PlcSeries.KV8000);
        CollectionAssert.AreEqual(Bytes("RDS T0.D 2\r"), command.ToAsciiBytes());

        var values = command.Execute(MockPlc("1,0000000010,0000000100 0,0000000000,0000000200\r\n"));
        Assert.AreEqual(2, values.Length);
        Assert.IsTrue(values[0].Contact);
        Assert.AreEqual(10u, values[0].Current);
        Assert.AreEqual(100u, values[0].Set);
        Assert.IsFalse(values[1].Contact);
        Assert.AreEqual(200u, values[1].Set);

        // 区切りがすべて空白の場合も解釈する
        var spaced = command.Execute(MockPlc("1 0000000010 0000000100 0 0000000000 0000000200\r\n"));
        Assert.AreEqual(200u, spaced[1].Set);

        Assert.ThrowsException<NotSupportedException>(() => new ContinuousReadTimerCounterCommand(Prefix.DM, "0", 1, PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousReadTimerCounterCommand(Prefix.T, "0", 121, PlcSeries.KV8000));
    }
}
