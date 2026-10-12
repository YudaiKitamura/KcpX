using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace TestKcpX;

[TestClass]
public sealed class TestOperatingMode
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("?M\r"), new ModeReadCommand().ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("M0\r"), new ModeChangeCommand(OperatingMode.Program).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("M1\r"), new ModeChangeCommand(OperatingMode.Run).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidMode()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ModeChangeCommand((OperatingMode)2));
    }

    [TestMethod]
    public async Task TestKcpX()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        Assert.AreEqual(OperatingMode.Run, plc.ReadOperatingMode());

        plc.ChangeOperatingMode(OperatingMode.Program);
        Assert.AreEqual(OperatingMode.Program, transport.Core.Mode);
        Assert.AreEqual(OperatingMode.Program, await plc.ReadOperatingModeAsync());

        await plc.ChangeOperatingModeAsync(OperatingMode.Run);
        Assert.AreEqual(OperatingMode.Run, plc.ReadOperatingMode());

        CollectionAssert.AreEqual(new[] { "?M", "M0", "?M", "M1", "?M" }, transport.Core.Requests);
    }

    [TestMethod]
    public void TestErrorResponse()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        transport.Core.ErrorCode = HostLinkErrorCode.E2;
        var ex = Assert.ThrowsException<HostLinkException>(() => plc.ChangeOperatingMode(OperatingMode.Run));
        Assert.AreEqual(HostLinkErrorCode.E2, ex.ErrorCode);
    }

    [TestMethod]
    public void TestInvalidResponse()
    {
        var transport = new FakePlcTransport();
        transport.Core.Mode = (OperatingMode)2;
        var plc = new KcpX(transport);
        Assert.ThrowsException<ReceivePacketException>(() => plc.ReadOperatingMode());
    }
}
