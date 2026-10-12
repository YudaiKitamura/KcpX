using System.Text;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using Moq;

namespace TestKcpX;

[TestClass]
public sealed class TestWriteCommand
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
        CollectionAssert.AreEqual(Bytes("WR DM100.S -5400\r"), new WriteCommand<short>(Prefix.DM, "100", -5400).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WR R1015 1\r"), new WriteCommand<bool>(Prefix.R, "1015", true).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WR DM0.D 1065353216\r"), new WriteCommand<float>(Prefix.DM, "0", 1.0f).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WR T0.D 100\r"), new WriteCommand<uint>(Prefix.T, "0", 100).ToAsciiBytes());
    }

    [TestMethod]
    public async Task TestExecute()
    {
        Assert.IsTrue(new WriteCommand<short>(Prefix.DM, "0", 1).Execute(MockPlc("OK\r\n")));
        Assert.IsTrue(await new WriteCommand<short>(Prefix.DM, "0", 1).ExecuteAsync(MockPlc("OK\r\n")));
        var ex = Assert.ThrowsException<HostLinkException>(() => new WriteCommand<uint>(Prefix.T, "0", 1).Execute(MockPlc("E4\r\n")));
        Assert.AreEqual(HostLinkErrorCode.E4, ex.ErrorCode);
    }

    [TestMethod]
    public void TestInvalidParameter()
    {
        Assert.ThrowsException<NotSupportedException>(() => new WriteCommand<bool>(Prefix.T, "0", true));
        Assert.ThrowsException<NotSupportedException>(() => new WriteCommand<bool>(Prefix.DM, "0", true));
        Assert.ThrowsException<NotSupportedException>(() => new WriteCommand<double>(Prefix.DM, "0", 1));
    }

    [TestMethod]
    public void TestContinuousToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("WRS R100 4 1 0 1 0\r"),
            new ContinuousWriteCommand<bool>(Prefix.R, "100", [true, false, true, false], PlcSeries.KV8000).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WRS DM200.S 3 15025 -5400 200\r"),
            new ContinuousWriteCommand<short>(Prefix.DM, "200", [15025, -5400, 200], PlcSeries.KV8000).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WRS DM0.D 2 0 1\r"),
            new ContinuousWriteCommand<long>(Prefix.DM, "0", [1L << 32], PlcSeries.KV8000).ToAsciiBytes());
    }

    [TestMethod]
    public void TestContinuousLength()
    {
        _ = new ContinuousWriteCommand<ushort>(Prefix.DM, "0", new ushort[1000], PlcSeries.KV8000);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousWriteCommand<ushort>(Prefix.DM, "0", [], PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousWriteCommand<ushort>(Prefix.DM, "0", new ushort[1001], PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousWriteCommand<int>(Prefix.DM, "0", new int[501], PlcSeries.KV8000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousWriteCommand<double>(Prefix.DM, "0", new double[65], PlcSeries.KVNano));
    }
}
