using KcpXLib;
using KcpXLib.Enums;

// KcpX 通信インスタンスを生成（引数でIPアドレスを変更可能）
// Create a KcpX communication instance (the IP address can be changed with an argument)
// - IPアドレス: 192.168.12.89 (IP address)
// - ポート番号: 8501 (Port number)
// - PLCシリーズ: KV-7500 / KV-7300 (PLC series)
var ip = args.Length > 0 ? args[0] : "192.168.12.89";
using var kcpx = new KcpX(ip, 8501, series: PlcSeries.KV7000);

// 内部リレー MR50000 に true を書き込み、読み取る
// Write true to internal relay MR50000 and read it back
kcpx.Write(Prefix.MR, "50000", true);
Console.WriteLine($"MR50000: {kcpx.Read<bool>(Prefix.MR, "50000")}");

// データメモリ DM17000 に short 型で -1234 を書き込み、読み取る
// Write -1234 as short to data memory DM17000 and read it back
kcpx.Write<short>(Prefix.DM, "17000", -1234);
Console.WriteLine($"DM17000: {kcpx.Read<short>(Prefix.DM, "17000")}");

// DM17002〜DM17003 に int 型、DM17004〜DM17005 に float 型、DM17010〜DM17013 に double 型で書き込み、読み取る
// Write int to DM17002-DM17003, float to DM17004-DM17005 and double to DM17010-DM17013, then read them back
kcpx.Write(Prefix.DM, "17002", 2147483647);
kcpx.Write(Prefix.DM, "17004", 3.14f);
kcpx.Write(Prefix.DM, "17010", Math.E);
Console.WriteLine($"DM17002: {kcpx.Read<int>(Prefix.DM, "17002")}");
Console.WriteLine($"DM17004: {kcpx.Read<float>(Prefix.DM, "17004")}");
Console.WriteLine($"DM17010: {kcpx.Read<double>(Prefix.DM, "17010")}");

// DM11000 から 2000 ワードを一括で書き込み、読み取る（1000 点ごとに自動分割）
// Batch write and read 2000 words from DM11000 (automatically split every 1000 points)
var values = Enumerable.Range(0, 2000).Select(i => (ushort)i).ToArray();
kcpx.BatchWrite(Prefix.DM, "11000", values);
ushort[] block = kcpx.BatchRead<ushort>(Prefix.DM, "11000", 2000);
Console.WriteLine($"DM11000-DM12999: {block[0]} ... {block[^1]}");

// 異なるデバイス・型をまとめて書き込み、読み取る
// Write and read different devices and types together
kcpx.Write(b => b
    .Add<short>(Prefix.DM, "17100", [1, 2, 3])
    .Add(Prefix.DM, "17110", 123456)
    .Add(Prefix.MR, "50010", true));

short[] shorts = [];
int dm17110 = 0;
bool mr50010 = false;
kcpx.Read(b => b
    .Add<short>(Prefix.DM, "17100", 3, v => shorts = v)
    .Add<int>(Prefix.DM, "17110", v => dm17110 = v)
    .Add<bool>(Prefix.MR, "50010", v => mr50010 = v));
Console.WriteLine($"DM17100-DM17102: {string.Join(", ", shorts)}, DM17110: {dm17110}, MR50010: {mr50010}");

// 強制セット／強制リセット
// Force set / force reset
kcpx.ForceSet(Prefix.MR, "50001");
kcpx.ForceReset(Prefix.MR, "50001");
Console.WriteLine($"MR50001: {kcpx.Read<bool>(Prefix.MR, "50001")}");

// モニタ登録したデバイスを繰り返し読み取る
// Read registered devices repeatedly with a monitor session
short monitorShort = 0;
bool monitorBit = false;
var session = kcpx.MonitorRegist(m => m
    .Add<short>(Prefix.DM, "17000", v => monitorShort = v)
    .Add<bool>(Prefix.MR, "50000", v => monitorBit = v));

for (int i = 0; i < 3; i++)
{
    kcpx.Write<short>(Prefix.DM, "17000", (short)i);
    session.Read();
    Console.WriteLine($"Monitor: DM17000={monitorShort}, MR50000={monitorBit}");
}
