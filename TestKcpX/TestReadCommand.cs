using System.Text;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using Moq;

namespace TestKcpX;

[TestClass]
public sealed class TestReadCommand
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    private static Mock<IPlc> MockPlc(string response)
    {
        var plc = new Mock<IPlc>();
        plc.Setup(p => p.Request(It.IsAny<byte[]>())).Returns(Bytes(response));
        plc.Setup(p => p.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(Bytes(response));
        plc.SetupGet(p => p.Series).Returns(PlcSeries.KV8000);
        return plc;
    }

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("RD DM100.S\r"), new ReadCommand<short>(Prefix.DM, "100").ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RD R1015\r"), new ReadCommand<bool>(Prefix.R, "1015").ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RD W1F.D\r"), new ReadCommand<float>(Prefix.W, "1f").ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RD T10.D\r"), new ReadCommand<bool>(Prefix.T, "10").ToAsciiBytes());
    }

    [TestMethod]
    public async Task TestExecute()
    {
        Assert.AreEqual((short)-25400, new ReadCommand<short>(Prefix.DM, "100").Execute(MockPlc("-25400\r\n").Object));
        Assert.AreEqual(4294967295u, await new ReadCommand<uint>(Prefix.DM, "100").ExecuteAsync(MockPlc("4294967295\r\n").Object));
        Assert.IsTrue(new ReadCommand<bool>(Prefix.T, "0").Execute(MockPlc("1,0000000010,0000000100\r\n").Object));
    }

    [TestMethod]
    public void TestExecuteError()
    {
        var ex = Assert.ThrowsException<HostLinkException>(() => new ReadCommand<short>(Prefix.DM, "100").Execute(MockPlc("E0\r\n").Object));
        Assert.AreEqual(HostLinkErrorCode.E0, ex.ErrorCode);
    }

    [TestMethod]
    public void TestInvalidParameter()
    {
        Assert.ThrowsException<NotSupportedException>(() => new ReadCommand<bool>(Prefix.DM, "0"));
        Assert.ThrowsException<NotSupportedException>(() => new ReadCommand<short>(Prefix.T, "0"));
        Assert.ThrowsException<NotSupportedException>(() => new ReadCommand<long>(Prefix.DM, "0"));
        Assert.ThrowsException<DeviceAddressException>(() => new ReadCommand<bool>(Prefix.R, "16"));
    }
}
