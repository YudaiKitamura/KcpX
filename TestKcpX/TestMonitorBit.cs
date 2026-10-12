using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace TestKcpX;

[TestClass]
public sealed class TestMonitorBit
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("MBS R100 MR1015 T5 CTC1\r"),
            new MonitorBitRegistCommand([(Prefix.R, "100"), (Prefix.MR, "1015"), (Prefix.T, "5"), (Prefix.CTC, "1")]).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("MBR\r"), new MonitorBitReadCommand(1).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidParameter()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new MonitorBitRegistCommand([]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new MonitorBitRegistCommand(Enumerable.Range(0, 121).Select(i => (Prefix.B, i.ToString("X"))).ToArray()));
        Assert.ThrowsException<NotSupportedException>(() => new MonitorBitRegistCommand([(Prefix.DM, "0")]));
        Assert.ThrowsException<NotSupportedException>(() => new MonitorBitRegistCommand([(Prefix.CTH, "0")]));
    }

    [TestMethod]
    public async Task TestSession()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        bool r0 = false, b1 = true, t3 = false;
        var session = plc.MonitorRegist(m => m
            .Add<bool>(Prefix.R, "0", v => r0 = v)
            .Add<bool>(Prefix.B, "1", v => b1 = v)
            .Add<bool>(Prefix.T, "3", v => t3 = v));

        plc.ForceSet(Prefix.R, "0");
        plc.ForceSet(Prefix.T, "3");
        session.Read();
        Assert.IsTrue(r0);
        Assert.IsFalse(b1);
        Assert.IsTrue(t3);

        plc.ForceReset(Prefix.R, "0");
        await session.ReadAsync();
        Assert.IsFalse(r0);

        CollectionAssert.AreEqual(new[] { "MBS R000 B1 T3", "ST R000", "ST T3", "MBR", "RS R000", "MBR" }, transport.Core.Requests);
    }

    [TestMethod]
    public async Task TestOldSessionInvalidated()
    {
        var plc = new KcpX(new FakePlcTransport());
        var first = plc.MonitorRegist(m => m.Add<bool>(Prefix.R, "0", _ => { }));
        _ = await plc.MonitorRegistAsync(m => m.Add<bool>(Prefix.R, "1", _ => { }));

        Assert.ThrowsException<InvalidOperationException>(() => first.Read());
    }

    [TestMethod]
    public void TestBuilderValidation()
    {
        var plc = new KcpX(new FakePlcTransport());

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => plc.MonitorRegist(_ => { }));
        Assert.ThrowsException<NotSupportedException>(() => plc.MonitorRegist(m => m.Add<bool>(Prefix.DM, "0", _ => { })));
        Assert.ThrowsException<DeviceAddressException>(() => plc.MonitorRegist(m => m.Add<bool>(Prefix.R, "16", _ => { })));
    }

    [TestMethod]
    public void TestReadWithoutRegistration()
    {
        var transport = new FakePlcTransport();
        var ex = Assert.ThrowsException<HostLinkException>(() => new MonitorBitReadCommand(1).Execute(new KcpX(transport)));
        Assert.AreEqual(HostLinkErrorCode.E0, ex.ErrorCode);
    }
}
