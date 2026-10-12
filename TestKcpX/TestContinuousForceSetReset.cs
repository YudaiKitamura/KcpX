using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestContinuousForceSetReset
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("STS R100 16\r"), new ContinuousForceSetResetCommand(Prefix.R, "100", 16, isSet: true).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RSS VB0 1\r"), new ContinuousForceSetResetCommand(Prefix.VB, "0", 1, isSet: false).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidParameter()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousForceSetResetCommand(Prefix.R, "0", 0, isSet: true));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousForceSetResetCommand(Prefix.R, "0", 17, isSet: true));
        Assert.ThrowsException<NotSupportedException>(() => new ContinuousForceSetResetCommand(Prefix.T, "0", 1, isSet: false));
        Assert.ThrowsException<NotSupportedException>(() => new ContinuousForceSetResetCommand(Prefix.DM, "0", 1, isSet: true));
    }

    [TestMethod]
    public async Task TestKcpX()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        plc.BatchForceSet(Prefix.R, "1000", 40);
        CollectionAssert.AreEqual(Enumerable.Repeat(true, 40).ToArray(), plc.BatchRead<bool>(Prefix.R, "1000", 40));
        Assert.IsFalse(plc.Read<bool>(Prefix.R, "1208"));

        await plc.BatchForceResetAsync(Prefix.R, "1004", 3);
        await plc.ContinuousForceResetAsync(Prefix.R, "1100", 2);
        CollectionAssert.AreEqual(
            new[] { true, false, false, false, true },
            plc.BatchRead<bool>(Prefix.R, "1003", 5));
        CollectionAssert.AreEqual(
            new[] { "STS R1000 16", "STS R1100 16", "STS R1200 8", "RDS R1000 40", "RD R1208", "RSS R1004 3", "RSS R1100 2", "RDS R1003 5" },
            transport.Core.Requests);
    }
}
