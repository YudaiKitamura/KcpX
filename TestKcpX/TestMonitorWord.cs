using System.Text;
using KcpXLib;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace TestKcpX;

[TestClass]
public sealed class TestMonitorWord
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Bytes("MWS DM100.U R1000.S TC3.D W1F.L\r"),
            new MonitorWordRegistCommand([
                (Prefix.DM, "100", DataFormat.U),
                (Prefix.R, "1000", DataFormat.S),
                (Prefix.TC, "3", DataFormat.D),
                (Prefix.W, "1F", DataFormat.L),
            ]).ToAsciiBytes());
        CollectionAssert.AreEqual(Bytes("MWR\r"), new MonitorWordReadCommand(1).ToAsciiBytes());
    }

    [TestMethod]
    public void TestInvalidParameter()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new MonitorWordRegistCommand([]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new MonitorWordRegistCommand(Enumerable.Range(0, 121).Select(i => (Prefix.DM, i.ToString(), DataFormat.U)).ToArray()));
        Assert.ThrowsException<NotSupportedException>(() => new MonitorWordRegistCommand([(Prefix.T, "0", DataFormat.D)]));
        Assert.ThrowsException<NotSupportedException>(() => new MonitorWordRegistCommand([(Prefix.C, "0", DataFormat.D)]));
        Assert.ThrowsException<DeviceAddressException>(() => new MonitorWordRegistCommand([(Prefix.R, "101", DataFormat.U)]));
    }

    [TestMethod]
    public async Task TestSession()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);

        ushort dm0 = 0;
        short dm1 = 0;
        int dm2 = 0;
        float dm4 = 0;
        double dm10 = 0;
        long dm20 = 0;
        ushort r100 = 0;
        uint tc3 = 0;
        bool r0 = false;

        var session = plc.MonitorRegist(m => m
            .Add<ushort>(Prefix.DM, "0", v => dm0 = v)
            .Add<short>(Prefix.DM, "1", v => dm1 = v)
            .Add<int>(Prefix.DM, "2", v => dm2 = v)
            .Add<float>(Prefix.DM, "4", v => dm4 = v)
            .Add<double>(Prefix.DM, "10", v => dm10 = v)
            .Add<long>(Prefix.DM, "20", v => dm20 = v)
            .Add<ushort>(Prefix.R, "100", v => r100 = v)
            .Add<uint>(Prefix.TC, "3", v => tc3 = v)
            .Add<bool>(Prefix.R, "0", v => r0 = v));

        plc.Write<ushort>(Prefix.DM, "0", 65535);
        plc.Write<short>(Prefix.DM, "1", -1234);
        plc.Write(Prefix.DM, "2", -123456789);
        plc.Write(Prefix.DM, "4", 3.5f);
        plc.Write(Prefix.DM, "10", Math.PI);
        plc.Write(Prefix.DM, "20", long.MinValue + 1);
        plc.Write<ushort>(Prefix.R, "100", 0xA5A5);
        transport.Core.Elements[(Prefix.TC, 3)] = 4000000000;
        plc.ForceSet(Prefix.R, "0");

        session.Read();
        Assert.AreEqual((ushort)65535, dm0);
        Assert.AreEqual((short)-1234, dm1);
        Assert.AreEqual(-123456789, dm2);
        Assert.AreEqual(3.5f, dm4);
        Assert.AreEqual(Math.PI, dm10);
        Assert.AreEqual(long.MinValue + 1, dm20);
        Assert.AreEqual((ushort)0xA5A5, r100);
        Assert.AreEqual(4000000000u, tc3);
        Assert.IsTrue(r0);

        plc.Write<ushort>(Prefix.DM, "0", 1);
        await session.ReadAsync();
        Assert.AreEqual((ushort)1, dm0);

        Assert.AreEqual("MBS R000", transport.Core.Requests[0]);
        Assert.AreEqual("MWS DM0.U DM1.S DM2.L DM4.D DM10.D DM12.D DM20.D DM22.D R100.U TC3.D", transport.Core.Requests[1]);
        CollectionAssert.AreEqual(new[] { "MBR", "MWR" }, transport.Core.Requests.Skip(transport.Core.Requests.Count - 2).ToArray());
    }

    [TestMethod]
    public void TestWordOnlySession()
    {
        var transport = new FakePlcTransport();
        var plc = new KcpX(transport);
        transport.Core.Elements[(Prefix.CTC, 0)] = 1000;

        uint ctc0 = 0;
        var session = plc.MonitorRegist(m => m.Add<uint>(Prefix.CTC, "0", v => ctc0 = v));
        session.Read();

        Assert.AreEqual(1000u, ctc0);
        CollectionAssert.AreEqual(new[] { "MWS CTC0.D", "MWR" }, transport.Core.Requests);
    }

    [TestMethod]
    public void TestBuilderValidation()
    {
        var plc = new KcpX(new FakePlcTransport());

        Assert.ThrowsException<NotSupportedException>(() => plc.MonitorRegist(m => m.Add<ushort>(Prefix.T, "0", _ => { })));
        Assert.ThrowsException<NotSupportedException>(() => plc.MonitorRegist(m => m.Add<double>(Prefix.TC, "0", _ => { })));
        Assert.ThrowsException<DeviceAddressException>(() => plc.MonitorRegist(m => m.Add<ushort>(Prefix.R, "101", _ => { })));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => plc.MonitorRegist(m =>
            {
                for (int i = 0; i < 61; i++)
                {
                    m.Add<long>(Prefix.DM, (i * 4).ToString(), _ => { });
                }
            }));
    }

    [TestMethod]
    public void TestReadWithoutRegistration()
    {
        var ex = Assert.ThrowsException<HostLinkException>(() => new MonitorWordReadCommand(1).Execute(new KcpX(new FakePlcTransport())));
        Assert.AreEqual(HostLinkErrorCode.E0, ex.ErrorCode);
    }
}
