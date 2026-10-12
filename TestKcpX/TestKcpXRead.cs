using KcpXLib;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace TestKcpX;

[TestClass]
public sealed class TestKcpXRead
{
    private static (KcpX Plc, FakeKvCore Core) Create(PlcSeries series = PlcSeries.KV8000)
    {
        var transport = new FakePlcTransport();
        return (new KcpX(transport, series), transport.Core);
    }

    [TestMethod]
    public async Task TestRead()
    {
        var (plc, core) = Create();
        core.SetRaw(Prefix.DM, 100, DataFormat.S, unchecked((uint)-123));
        core.SetRaw(Prefix.DM, 200, DataFormat.D, 0x40490FDB);
        core.SetRaw(Prefix.MR, 1 * 16 + 5, DataFormat.Bit, 1);
        core.SetRaw(Prefix.EM, 0, DataFormat.D, 0x89ABCDEF);
        core.SetRaw(Prefix.EM, 2, DataFormat.D, 0x01234567);

        Assert.AreEqual((short)-123, plc.Read<short>(Prefix.DM, "100"));
        Assert.AreEqual(3.14159274f, await plc.ReadAsync<float>(Prefix.DM, "200"));
        Assert.IsTrue(plc.Read<bool>(Prefix.MR, "105"));
        Assert.AreEqual(0x0123456789ABCDEFUL, plc.Read<ulong>(Prefix.EM, "0"));
        CollectionAssert.AreEqual(new[] { "RD DM100.S", "RD DM200.D", "RD MR105", "RDS EM0.D 2" }, core.Requests);
    }

    [TestMethod]
    public void TestBatchReadSplit()
    {
        var (plc, core) = Create();
        for (uint i = 0; i < 2500; i++)
        {
            core.SetRaw(Prefix.DM, i, DataFormat.U, i);
        }

        var values = plc.BatchRead<ushort>(Prefix.DM, "0", 2500);

        CollectionAssert.AreEqual(Enumerable.Range(0, 2500).Select(i => (ushort)i).ToArray(), values);
        CollectionAssert.AreEqual(new[] { "RDS DM0.U 1000", "RDS DM1000.U 1000", "RDS DM2000.U 500" }, core.Requests);
    }

    [TestMethod]
    public async Task TestBatchReadSplitDouble()
    {
        var (plc, core) = Create(PlcSeries.KVNano);
        for (uint i = 0; i < 300; i++)
        {
            core.SetRaw(Prefix.DM, i * 4, DataFormat.D, i);
            core.SetRaw(Prefix.DM, i * 4 + 2, DataFormat.D, 0);
        }

        var values = await plc.BatchReadAsync<long>(Prefix.DM, "0", 300);

        CollectionAssert.AreEqual(Enumerable.Range(0, 300).Select(i => (long)i).ToArray(), values);
        CollectionAssert.AreEqual(new[] { "RDS DM0.D 128", "RDS DM256.D 128", "RDS DM512.D 128", "RDS DM768.D 128", "RDS DM1024.D 88" }, core.Requests);
    }

    [TestMethod]
    public void TestBatchReadRelaySplit()
    {
        var (plc, core) = Create();
        core.SetRaw(Prefix.R, 1000 / 100 * 16 + 1000 % 100, DataFormat.Bit, 1);

        var bits = plc.BatchRead<bool>(Prefix.R, "0", 1001);
        Assert.IsTrue(bits[1000 / 100 * 16]);
        CollectionAssert.AreEqual(new[] { "RDS R000 1000", "RDS R6208 1" }, core.Requests);

        core.Requests.Clear();
        core.SetRaw(Prefix.R, 1000 * 16, DataFormat.U, 0xBEEF);
        var words = plc.BatchRead<ushort>(Prefix.R, "0", 1001);
        Assert.AreEqual(0xBEEF, words[1000]);
        CollectionAssert.AreEqual(new[] { "RDS R000.U 1000", "RDS R100000.U 1" }, core.Requests);
    }

    [TestMethod]
    public void TestBatchReadTimerCounter()
    {
        var (plc, core) = Create();
        core.SetRaw(Prefix.TC, 130, DataFormat.D, 5);
        core.SetRaw(Prefix.TS, 130, DataFormat.D, 50);
        core.Bits[(Prefix.T, 130)] = true;

        var values = plc.BatchReadTimerCounter(Prefix.T, "0", 200);

        Assert.AreEqual(200, values.Length);
        Assert.IsTrue(values[130].Contact);
        Assert.AreEqual(5u, values[130].Current);
        Assert.AreEqual(50u, values[130].Set);
        Assert.IsTrue(plc.Read<bool>(Prefix.T, "130"));
        Assert.AreEqual(50u, plc.ReadTimerCounter(Prefix.T, "130").Set);
        CollectionAssert.AreEqual(new[] { "RDS T0.D 120", "RDS T120.D 80", "RD T130.D", "RDS T130.D 1" }, core.Requests);
    }

    [TestMethod]
    public void TestReadBuilder()
    {
        var (plc, core) = Create();
        core.SetRaw(Prefix.DM, 0, DataFormat.U, 7);
        core.SetRaw(Prefix.B, 0x10, DataFormat.Bit, 1);

        ushort single = 0;
        bool[] bits = [];
        plc.Read(b => b
            .Add<ushort>(Prefix.DM, "0", v => single = v)
            .Add<bool>(Prefix.B, "F", 2, v => bits = v));

        Assert.AreEqual((ushort)7, single);
        CollectionAssert.AreEqual(new[] { false, true }, bits);
    }

    [TestMethod]
    public void TestErrorResponse()
    {
        var (plc, core) = Create();
        core.ErrorCode = HostLinkErrorCode.E0;

        var ex = Assert.ThrowsException<HostLinkException>(() => plc.Read<short>(Prefix.DM, "0"));
        Assert.AreEqual(HostLinkErrorCode.E0, ex.ErrorCode);
    }
}
