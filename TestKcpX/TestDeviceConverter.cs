using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Utils;

namespace TestKcpX;

[TestClass]
public sealed class TestDeviceConverter
{
    [TestMethod]
    [DataRow(Prefix.R, "0", 0u)]
    [DataRow(Prefix.R, "15", 15u)]
    [DataRow(Prefix.R, "100", 16u)]
    [DataRow(Prefix.MR, "199915", 1999u * 16 + 15)]
    [DataRow(Prefix.B, "1F", 31u)]
    [DataRow(Prefix.W, "7fff", 0x7FFFu)]
    [DataRow(Prefix.DM, "65534", 65534u)]
    public void TestToIndex(Prefix prefix, string address, uint expected)
    {
        Assert.AreEqual(expected, DeviceConverter.ToIndex(prefix, address));
    }

    [TestMethod]
    [DataRow(Prefix.R, "16")]
    [DataRow(Prefix.MR, "199")]
    [DataRow(Prefix.DM, "1A")]
    [DataRow(Prefix.DM, "-1")]
    [DataRow(Prefix.DM, "")]
    [DataRow(Prefix.B, "G0")]
    public void TestToIndexInvalid(Prefix prefix, string address)
    {
        Assert.ThrowsException<DeviceAddressException>(() => DeviceConverter.ToIndex(prefix, address));
    }

    [TestMethod]
    [DataRow(Prefix.R, "015", 1, "100")]
    [DataRow(Prefix.R, "002", 16, "102")]
    [DataRow(Prefix.LR, "1000", 32, "1200")]
    [DataRow(Prefix.B, "F", 1, "10")]
    [DataRow(Prefix.DM, "100", 1000, "1100")]
    public void TestGetOffsetAddress(Prefix prefix, string address, int offset, string expected)
    {
        Assert.AreEqual(expected, DeviceConverter.GetOffsetAddress(prefix, address, offset));
    }

    [TestMethod]
    public void TestToDeviceText()
    {
        Assert.AreEqual("DM200.S", DeviceConverter.ToDeviceText(Prefix.DM, "0200", DataFormat.S));
        Assert.AreEqual("R100", DeviceConverter.ToDeviceText(Prefix.R, "100", DataFormat.Bit));
        Assert.AreEqual("W1A.U", DeviceConverter.ToDeviceText(Prefix.W, "1a", DataFormat.U));
        Assert.AreEqual("R005.U", DeviceConverter.ToDeviceText(Prefix.R, "5", DataFormat.U));
    }

    [TestMethod]
    public void TestGetMaxPoints()
    {
        Assert.AreEqual(1000, DeviceConverter.GetMaxPoints(Prefix.DM, DataFormat.U, PlcSeries.KV8000));
        Assert.AreEqual(500, DeviceConverter.GetMaxPoints(Prefix.DM, DataFormat.L, PlcSeries.KV8000));
        Assert.AreEqual(1000, DeviceConverter.GetMaxPoints(Prefix.R, DataFormat.Bit, PlcSeries.KV5000));
        Assert.AreEqual(512, DeviceConverter.GetMaxPoints(Prefix.TM, DataFormat.U, PlcSeries.KV7000));
        Assert.AreEqual(256, DeviceConverter.GetMaxPoints(Prefix.TM, DataFormat.D, PlcSeries.KV7000));
        Assert.AreEqual(120, DeviceConverter.GetMaxPoints(Prefix.T, DataFormat.D, PlcSeries.KV8000));
        Assert.AreEqual(12, DeviceConverter.GetMaxPoints(Prefix.Z, DataFormat.U, PlcSeries.KV8000));
        Assert.AreEqual(256, DeviceConverter.GetMaxPoints(Prefix.DM, DataFormat.U, PlcSeries.KVNano));
        Assert.AreEqual(128, DeviceConverter.GetMaxPoints(Prefix.DM, DataFormat.D, PlcSeries.KVNano));
        Assert.AreEqual(64, DeviceConverter.GetMaxPoints(Prefix.CC, DataFormat.D, PlcSeries.KVNano));
    }
}
