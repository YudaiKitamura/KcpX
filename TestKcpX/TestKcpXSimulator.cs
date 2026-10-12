using System.Net;
using System.Net.Sockets;
using KcpXLib;
using KcpXLib.Enums;

namespace TestKcpX;

[TestClass]
public sealed class TestKcpXSimulator
{
    [TestMethod]
    public void TestConnect()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            using var simulator = new KcpXSimulator(port: ((IPEndPoint)listener.LocalEndpoint).Port);
            Assert.AreEqual(PlcSeries.KV8000, simulator.Series);
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void TestReadWrite()
    {
        var transport = new FakePlcTransport();
        using var simulator = new KcpXSimulator(transport);

        simulator.Write(Prefix.DM, "100", (short)-1234);
        Assert.AreEqual((short)-1234, simulator.Read<short>(Prefix.DM, "100"));
        CollectionAssert.AreEqual(new[] { "WR DM100.S -1234", "RD DM100.S" }, transport.Core.Requests);
    }

    [TestMethod]
    public async Task TestMonitorNotSupported()
    {
        var transport = new FakePlcTransport();
        using var simulator = new KcpXSimulator(transport);

        Assert.ThrowsException<NotSupportedException>(() => simulator.MonitorRegist(m => m.Add<bool>(Prefix.R, "000", _ => { })));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => simulator.MonitorRegistAsync(m => m.Add<short>(Prefix.DM, "0", _ => { })));

        KcpX kcpx = simulator;
        Assert.ThrowsException<NotSupportedException>(() => kcpx.MonitorRegist(m => m.Add<bool>(Prefix.R, "000", _ => { })));

        Assert.AreEqual(0, transport.Core.Requests.Count);
    }
}
