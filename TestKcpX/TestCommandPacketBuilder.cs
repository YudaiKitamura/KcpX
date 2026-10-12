using System.Text;
using KcpXLib.Builders;

namespace TestKcpX;

[TestClass]
public sealed class TestCommandPacketBuilder
{
    [TestMethod]
    public void TestToAsciiBytes()
    {
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("RDS DM200.S 3\r"), new CommandPacketBuilder("RDS", "DM200.S", "3").ToAsciiBytes());
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("MBR\r"), new CommandPacketBuilder("MBR").ToAsciiBytes());
    }
}
