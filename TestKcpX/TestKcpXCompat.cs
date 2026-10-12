using KcpXLib;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestKcpXCompat
{
    [TestMethod]
    public async Task TestWriteRead()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        plc.WriteBool(Prefix.MR, "0", true);
        plc.WriteInt16(Prefix.DM, "0", -2);
        plc.WriteUInt16(Prefix.DM, "1", 65535);
        plc.WriteInt32(Prefix.DM, "2", -123456);
        plc.WriteUInt32(Prefix.DM, "4", 4000000000);
        plc.WriteInt64(Prefix.DM, "10", long.MinValue);
        plc.WriteUInt64(Prefix.DM, "20", ulong.MaxValue);
        plc.WriteSingle(Prefix.DM, "30", 1.5f);
        await plc.WriteDoubleAsync(Prefix.DM, "40", -2.25);

        Assert.IsTrue(plc.ReadBool(Prefix.MR, "0"));
        Assert.AreEqual((short)-2, plc.ReadInt16(Prefix.DM, "0"));
        Assert.AreEqual((ushort)65535, plc.ReadUInt16(Prefix.DM, "1"));
        Assert.AreEqual(-123456, plc.ReadInt32(Prefix.DM, "2"));
        Assert.AreEqual(4000000000u, plc.ReadUInt32(Prefix.DM, "4"));
        Assert.AreEqual(long.MinValue, plc.ReadInt64(Prefix.DM, "10"));
        Assert.AreEqual(ulong.MaxValue, plc.ReadUInt64(Prefix.DM, "20"));
        Assert.AreEqual(1.5f, plc.ReadSingle(Prefix.DM, "30"));
        Assert.AreEqual(-2.25, await plc.ReadDoubleAsync(Prefix.DM, "40"));
    }

    [TestMethod]
    public async Task TestBatchWriteRead()
    {
        var plc = new KcpX(new FakePlcTransport());

        plc.BatchWriteInt32(Prefix.DM, "100", [1, -2, 3]);
        await plc.BatchWriteBoolAsync(Prefix.MR, "100", [true, false, true]);

        CollectionAssert.AreEqual(new[] { 1, -2, 3 }, plc.BatchReadInt32(Prefix.DM, "100", 3));
        CollectionAssert.AreEqual(new[] { true, false, true }, await plc.BatchReadBoolAsync(Prefix.MR, "100", 3));
    }
}
