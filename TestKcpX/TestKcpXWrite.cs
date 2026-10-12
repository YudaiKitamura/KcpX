using KcpXLib;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestKcpXWrite
{
    private static (KcpX Plc, FakeKvCore Core) Create(PlcSeries series = PlcSeries.KV8000)
    {
        var transport = new FakePlcTransport();
        return (new KcpX(transport, series), transport.Core);
    }

    [TestMethod]
    public async Task TestWriteRoundTrip()
    {
        var (plc, core) = Create();

        plc.Write<short>(Prefix.DM, "0", -1234);
        await plc.WriteAsync(Prefix.DM, "2", 3.5f);
        plc.Write(Prefix.EM, "10", -2.25);
        plc.Write(Prefix.LR, "1015", true);
        plc.Write<ushort>(Prefix.W, "1F", 0xABCD);

        Assert.AreEqual((short)-1234, plc.Read<short>(Prefix.DM, "0"));
        Assert.AreEqual(3.5f, plc.Read<float>(Prefix.DM, "2"));
        Assert.AreEqual(-2.25, await plc.ReadAsync<double>(Prefix.EM, "10"));
        Assert.IsTrue(plc.Read<bool>(Prefix.LR, "1015"));
        Assert.AreEqual((ushort)0xABCD, plc.Read<ushort>(Prefix.W, "1F"));
        CollectionAssert.AreEqual(
            new[] { "WR DM0.S -1234", "WR DM2.D 1080033280", "WRS EM10.D 2 0 3221356544", "WR LR1015 1", "WR W1F.U 43981" },
            core.Requests.Take(5).ToArray());
    }

    [TestMethod]
    public void TestWriteTimerCounter()
    {
        var (plc, _) = Create();

        plc.Write<uint>(Prefix.T, "5", 77);

        Assert.AreEqual(77u, plc.ReadTimerCounter(Prefix.T, "5").Current);
    }

    [TestMethod]
    public async Task TestBatchWriteSplit()
    {
        var (plc, core) = Create();
        var values = Enumerable.Range(0, 1200).Select(i => i * 3).ToArray();

        await plc.BatchWriteAsync(Prefix.ZF, "100", values);

        CollectionAssert.AreEqual(values, plc.BatchRead<int>(Prefix.ZF, "100", 1200));
        Assert.AreEqual(3, core.Requests.Count(r => r.StartsWith("WRS")));
        StringAssert.StartsWith(core.Requests[0], "WRS ZF100.L 500 0 3 6");
        StringAssert.StartsWith(core.Requests[1], "WRS ZF1100.L 500 1500");
        StringAssert.StartsWith(core.Requests[2], "WRS ZF2100.L 200 3000");
    }

    [TestMethod]
    public void TestBatchWriteBits()
    {
        var (plc, core) = Create();
        var values = Enumerable.Range(0, 1010).Select(i => i % 3 == 0).ToArray();

        plc.BatchWrite(Prefix.MR, "500", values);

        CollectionAssert.AreEqual(values, plc.BatchRead<bool>(Prefix.MR, "500", 1010));
        StringAssert.StartsWith(core.Requests[1], "WRS MR6708 10 ");
    }

    [TestMethod]
    public void TestWriteBuilder()
    {
        var (plc, _) = Create();

        plc.Write(b => b
            .Add<short>(Prefix.DM, "0", 1)
            .Add(Prefix.DM, "10", new uint[] { 2, 3 })
            .Add(Prefix.B, "A", true));

        Assert.AreEqual((short)1, plc.Read<short>(Prefix.DM, "0"));
        CollectionAssert.AreEqual(new uint[] { 2, 3 }, plc.BatchRead<uint>(Prefix.DM, "10", 2));
        Assert.IsTrue(plc.Read<bool>(Prefix.B, "A"));
    }
}
