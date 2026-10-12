using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestForceSetReset
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("ST R1015\r"), new ForceSetResetCommand(Prefix.R, "1015", isSet: true).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RS B1F\r"), new ForceSetResetCommand(Prefix.B, "1f", isSet: false).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("ST T5\r"), new ForceSetResetCommand(Prefix.T, "5", isSet: true).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("RS CTC2\r"), new ForceSetResetCommand(Prefix.CTC, "2", isSet: false).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidDevice()
    {
        Assert.ThrowsException<NotSupportedException>(() => new ForceSetResetCommand(Prefix.DM, "0", isSet: true));
        Assert.ThrowsException<NotSupportedException>(() => new ForceSetResetCommand(Prefix.CTC, "0", isSet: true));
        Assert.ThrowsException<NotSupportedException>(() => new ForceSetResetCommand(Prefix.CTH, "0", isSet: false));
    }

    [TestMethod]
    public async Task TestKcpX()
    {
        var plc = new KcpX(new FakePlcTransport());

        plc.ForceSet(Prefix.MR, "315");
        await plc.ForceSetAsync(Prefix.C, "1");
        Assert.IsTrue(plc.Read<bool>(Prefix.MR, "315"));
        Assert.IsTrue(plc.Read<bool>(Prefix.C, "1"));

        plc.ForceReset(Prefix.MR, "315");
        await plc.ForceResetAsync(Prefix.C, "1");
        Assert.IsFalse(plc.Read<bool>(Prefix.MR, "315"));
        Assert.IsFalse(plc.Read<bool>(Prefix.C, "1"));
    }
}
