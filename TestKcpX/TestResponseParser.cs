using System.Text;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Parsers;

namespace TestKcpX;

[TestClass]
public sealed class TestResponseParser
{
    private static byte[] Bytes(string text) => Encoding.ASCII.GetBytes(text);

    [TestMethod]
    public void TestParseValues()
    {
        CollectionAssert.AreEqual(
            new[] { "+15025", "-25400", "+00000" },
            ResponseParser.ParseValues(Bytes("+15025 -25400 +00000\r\n"), 3));
    }

    [TestMethod]
    public void TestParseValuesCountMismatch()
    {
        Assert.ThrowsException<ReceivePacketException>(() => ResponseParser.ParseValues(Bytes("1 0\r\n"), 3));
    }

    [TestMethod]
    public void TestParseOk()
    {
        ResponseParser.ParseOk(Bytes("OK\r\n"));
        Assert.ThrowsException<ReceivePacketException>(() => ResponseParser.ParseOk(Bytes("NG\r\n")));
    }

    [TestMethod]
    [DataRow("E0\r\n", HostLinkErrorCode.E0)]
    [DataRow("E1\r\n", HostLinkErrorCode.E1)]
    [DataRow("E4\r\n", HostLinkErrorCode.E4)]
    public void TestErrorResponse(string response, HostLinkErrorCode code)
    {
        var ex = Assert.ThrowsException<HostLinkException>(() => ResponseParser.Parse(Bytes(response)));
        Assert.AreEqual(code, ex.ErrorCode);
    }

    [TestMethod]
    public void TestMissingTerminator()
    {
        Assert.ThrowsException<ReceivePacketException>(() => ResponseParser.Parse(Bytes("OK\r")));
    }
}
