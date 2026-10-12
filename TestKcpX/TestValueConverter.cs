using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Utils;

namespace TestKcpX;

[TestClass]
public sealed class TestValueConverter
{
    [TestMethod]
    public void TestGetFormat()
    {
        Assert.AreEqual(DataFormat.Bit, ValueConverter.GetFormat<bool>());
        Assert.AreEqual(DataFormat.U, ValueConverter.GetFormat<ushort>());
        Assert.AreEqual(DataFormat.S, ValueConverter.GetFormat<short>());
        Assert.AreEqual(DataFormat.D, ValueConverter.GetFormat<uint>());
        Assert.AreEqual(DataFormat.L, ValueConverter.GetFormat<int>());
        Assert.AreEqual(DataFormat.D, ValueConverter.GetFormat<float>());
        Assert.AreEqual(DataFormat.D, ValueConverter.GetFormat<double>());
        Assert.ThrowsException<NotSupportedException>(() => ValueConverter.GetFormat<byte>());
    }

    [TestMethod]
    public void TestGetStride()
    {
        Assert.AreEqual(1, ValueConverter.GetStride<bool>(Prefix.R));
        Assert.AreEqual(16, ValueConverter.GetStride<short>(Prefix.R));
        Assert.AreEqual(32, ValueConverter.GetStride<int>(Prefix.B));
        Assert.AreEqual(1, ValueConverter.GetStride<short>(Prefix.DM));
        Assert.AreEqual(2, ValueConverter.GetStride<float>(Prefix.DM));
        Assert.AreEqual(4, ValueConverter.GetStride<double>(Prefix.EM));
        Assert.AreEqual(1, ValueConverter.GetStride<uint>(Prefix.TC));
    }

    [TestMethod]
    public void TestValidate()
    {
        ValueConverter.Validate<bool>(Prefix.R);
        ValueConverter.Validate<bool>(Prefix.T);
        ValueConverter.Validate<short>(Prefix.R);
        ValueConverter.Validate<double>(Prefix.DM);
        ValueConverter.Validate<uint>(Prefix.TS);
        Assert.ThrowsException<NotSupportedException>(() => ValueConverter.Validate<bool>(Prefix.DM));
        Assert.ThrowsException<NotSupportedException>(() => ValueConverter.Validate<long>(Prefix.TC));
        Assert.ThrowsException<NotSupportedException>(() => ValueConverter.Validate<float>(Prefix.Z));
    }

    [TestMethod]
    public void TestRoundTrip()
    {
        AssertRoundTrip(new[] { true, false, true });
        AssertRoundTrip(new short[] { short.MinValue, 0, short.MaxValue });
        AssertRoundTrip(new ushort[] { 0, 65535 });
        AssertRoundTrip(new[] { int.MinValue, 0, int.MaxValue });
        AssertRoundTrip(new[] { 0u, uint.MaxValue });
        AssertRoundTrip(new[] { -1.5f, 3.14159f });
        AssertRoundTrip(new[] { long.MinValue, 1L, long.MaxValue });
        AssertRoundTrip(new[] { 0UL, ulong.MaxValue });
        AssertRoundTrip(new[] { -2.5, Math.PI });
    }

    private static void AssertRoundTrip<T>(T[] values) where T : unmanaged
    {
        CollectionAssert.AreEqual(values, ValueConverter.FromTokens<T>(ValueConverter.ToTokens(values)));
    }

    [TestMethod]
    public void TestToTokens()
    {
        CollectionAssert.AreEqual(new[] { "15025", "-5400" }, ValueConverter.ToTokens(new short[] { 15025, -5400 }));
        CollectionAssert.AreEqual(new[] { "1", "0" }, ValueConverter.ToTokens(new[] { 1UL, 0UL }).Take(2).ToArray());
        CollectionAssert.AreEqual(new[] { "0", "1" }, ValueConverter.ToTokens(new[] { 1UL << 32 }));
    }

    [TestMethod]
    public void TestFromTokens()
    {
        CollectionAssert.AreEqual(new short[] { 15025, -25400, 0 }, ValueConverter.FromTokens<short>(["+15025", "-25400", "+00000"]));
        CollectionAssert.AreEqual(new[] { -1 }, ValueConverter.FromTokens<int>(["-0000000001"]));
        CollectionAssert.AreEqual(new ushort[] { 1 }, ValueConverter.FromTokens<ushort>(["00001"]));
        Assert.ThrowsException<ReceivePacketException>(() => ValueConverter.FromTokens<bool>(["2"]));
        Assert.ThrowsException<ReceivePacketException>(() => ValueConverter.FromTokens<short>(["40000"]));
        Assert.ThrowsException<ReceivePacketException>(() => ValueConverter.FromTokens<long>(["1"]));
    }
}
