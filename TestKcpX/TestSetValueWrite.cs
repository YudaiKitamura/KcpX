using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestSetValueWrite
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("WS T10.D 500\r"), new WriteCommand<uint>(Prefix.T, "10", 500, isSetValue: true).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("WSS C0.U 2 1 2\r"),
            new ContinuousWriteCommand<ushort>(Prefix.C, "0", [1, 2], PlcSeries.KV8000, isSetValue: true).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidDevice()
    {
        Assert.ThrowsException<NotSupportedException>(() => new WriteCommand<uint>(Prefix.DM, "0", 1, isSetValue: true));
        Assert.ThrowsException<NotSupportedException>(() => new WriteCommand<uint>(Prefix.CTH, "0", 1, isSetValue: true));
        Assert.ThrowsException<NotSupportedException>(() => new ContinuousWriteCommand<uint>(Prefix.TS, "0", [1], PlcSeries.KV8000, isSetValue: true));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ContinuousWriteCommand<uint>(Prefix.T, "0", new uint[121], PlcSeries.KV8000, isSetValue: true));
    }

    [TestMethod]
    public async Task TestKcpX()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        plc.WriteSetValue<uint>(Prefix.T, "3", 1000);
        await plc.WriteSetValueAsync<uint>(Prefix.CTC, "1", 20);
        plc.BatchWriteSetValue(Prefix.C, "0", Enumerable.Range(0, 130).Select(i => (uint)i).ToArray());

        Assert.AreEqual(1000u, plc.ReadTimerCounter(Prefix.T, "3").Set);
        Assert.AreEqual(20u, plc.ReadTimerCounter(Prefix.CTC, "1").Set);
        Assert.AreEqual(129u, plc.Read<uint>(Prefix.CS, "129"));
        CollectionAssert.Contains(transport.Core.Requests, "WSS C120.D 10 120 121 122 123 124 125 126 127 128 129");
        Assert.ThrowsException<NotSupportedException>(() => plc.BatchWriteSetValue(Prefix.DM, "0", new uint[] { 1 }));
    }
}
