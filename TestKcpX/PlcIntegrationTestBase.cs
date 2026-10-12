using KcpXLib;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace TestKcpX;

/// <summary>
/// 実機 PLC を使った結合テストの共通実装。
/// </summary>
/// <remarks>
/// DM11000〜DM17199・MR50000〜MR53015を上書きします。<br/>
/// タイマ／カウンタのテストは KCPX_PLC_TIMER / KCPX_PLC_COUNTER で指定した番号の設定値・現在値を一時的に書き換え、終了時に元に戻します。接続先は派生クラスの <see cref="Connect"/> で決まります。
/// </remarks>
public abstract class PlcIntegrationTestBase
{
    private static readonly Random random = new();

    /// <summary>
    /// 接続先 PLC へ接続する。実行条件を満たさない場合は <see cref="Assert.Inconclusive(string)"/> を呼ぶ。
    /// </summary>
    protected abstract KcpX Connect();

    /// <summary>
    /// 接続先がモニタコマンド（MBS / MBR / MWS / MWR）に対応しているか。
    /// </summary>
    protected virtual bool SupportsMonitor => true;

    /// <summary>
    /// モード切替のテストを有効にする環境変数。<c>null</c> の場合は常に実行する。
    /// </summary>
    protected virtual string? ModeChangeEnableVar => null;

    /// <summary>
    /// 環境変数 <paramref name="enableVar"/> が 1 でなければ <see cref="Assert.Inconclusive(string)"/> で中断する。
    /// </summary>
    internal static void EnsureEnabled(string enableVar)
    {
        if (Environment.GetEnvironmentVariable(enableVar) != "1")
        {
            Assert.Inconclusive($"このテストは {enableVar}=1 のときのみ実行します。");
        }
    }

    /// <summary>
    /// 環境変数 <paramref name="name"/> の整数値（未指定・不正なら <paramref name="defaultValue"/>）。
    /// </summary>
    internal static int GetEnvironmentInt(string name, int defaultValue)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : defaultValue;

    /// <summary>
    /// 環境変数 <paramref name="enableVar"/> が 1 のときのみ、IP/ポートを環境変数（未指定なら既定値）から決めて接続する。
    /// </summary>
    internal static KcpX ConnectIfEnabled(string enableVar, string ipVar, string defaultIp, string portVar, int defaultPort, bool isUdp, PlcSeries series)
    {
        EnsureEnabled(enableVar);

        var ip = Environment.GetEnvironmentVariable(ipVar) ?? defaultIp;
        return new KcpX(ip, GetEnvironmentInt(portVar, defaultPort), isUdp, series: series);
    }

    private static T[] RandomValues<T>(int length, Func<T> next) => Enumerable.Range(0, length).Select(_ => next()).ToArray();

    private static short NextShort() => (short)random.Next(short.MinValue, short.MaxValue + 1);

    private static ushort NextUShort() => (ushort)random.Next(0, ushort.MaxValue + 1);

    private static int NextInt() => random.Next(int.MinValue, int.MaxValue);

    // ---------------------------------------------------------------
    // 単一デバイス（DM17000〜DM17099 / MR50000〜MR50015）
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestSingleWriteRead()
    {
        using var kcpx = Connect();

        ushort u = NextUShort();
        short s = NextShort();
        uint ui = (uint)random.NextInt64(0, uint.MaxValue + 1L);
        int i = NextInt();
        float f = (float)(random.NextDouble() * 1000 - 500);
        long l = random.NextInt64(long.MinValue, long.MaxValue);
        ulong ul = (ulong)random.NextInt64(long.MinValue, long.MaxValue);
        double d = random.NextDouble() * 1e6 - 5e5;
        bool bit = random.Next(2) == 1;

        kcpx.Write(Prefix.DM, "17000", u);
        kcpx.Write(Prefix.DM, "17001", s);
        kcpx.Write(Prefix.DM, "17010", ui);
        kcpx.Write(Prefix.DM, "17020", i);
        kcpx.Write(Prefix.DM, "17030", f);
        kcpx.Write(Prefix.DM, "17040", l);
        kcpx.Write(Prefix.DM, "17050", ul);
        kcpx.Write(Prefix.DM, "17060", d);
        kcpx.Write(Prefix.MR, "50000", bit);

        Assert.AreEqual(u, kcpx.Read<ushort>(Prefix.DM, "17000"));
        Assert.AreEqual(s, kcpx.Read<short>(Prefix.DM, "17001"));
        Assert.AreEqual(ui, kcpx.Read<uint>(Prefix.DM, "17010"));
        Assert.AreEqual(i, kcpx.Read<int>(Prefix.DM, "17020"));
        Assert.AreEqual(f, kcpx.Read<float>(Prefix.DM, "17030"));
        Assert.AreEqual(l, kcpx.Read<long>(Prefix.DM, "17040"));
        Assert.AreEqual(ul, kcpx.Read<ulong>(Prefix.DM, "17050"));
        Assert.AreEqual(d, kcpx.Read<double>(Prefix.DM, "17060"));
        Assert.AreEqual(bit, kcpx.Read<bool>(Prefix.MR, "50000"));

        // 32 ビット値は指定番号が下位ワード、次の番号が上位ワードであること。
        Assert.AreEqual((ushort)(ui & 0xFFFF), kcpx.Read<ushort>(Prefix.DM, "17010"));
        Assert.AreEqual((ushort)(ui >> 16), kcpx.Read<ushort>(Prefix.DM, "17011"));
    }

    [TestMethod]
    public async Task TestSingleWriteReadAsync()
    {
        using var kcpx = Connect();

        int i = NextInt();
        bool bit = random.Next(2) == 1;

        await kcpx.WriteAsync(Prefix.DM, "17070", i);
        await kcpx.WriteAsync(Prefix.MR, "50001", bit);

        Assert.AreEqual(i, await kcpx.ReadAsync<int>(Prefix.DM, "17070"));
        Assert.AreEqual(bit, await kcpx.ReadAsync<bool>(Prefix.MR, "50001"));
    }

    // ---------------------------------------------------------------
    // 連続デバイス（分割あり、DM11000〜DM16399 / MR50100〜MR52015）
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestBatchWordSplit()
    {
        using var kcpx = Connect();

        // 2500 ワード → 1000 + 1000 + 500 に分割される
        var values = RandomValues(2500, NextShort);
        kcpx.BatchWrite(Prefix.DM, "11000", values);

        CollectionAssert.AreEqual(values, kcpx.BatchRead<short>(Prefix.DM, "11000", 2500));

        // 分割境界（DM12000）をまたいで一致すること
        Assert.AreEqual(values[999], kcpx.Read<short>(Prefix.DM, "11999"));
        Assert.AreEqual(values[1000], kcpx.Read<short>(Prefix.DM, "12000"));
    }

    [TestMethod]
    public async Task TestBatchDoubleWordSplitAsync()
    {
        using var kcpx = Connect();

        // int 1200 要素 → 500 + 500 + 200 に分割される
        var values = RandomValues(1200, NextInt);
        await kcpx.BatchWriteAsync(Prefix.DM, "14000", values);

        CollectionAssert.AreEqual(values, await kcpx.BatchReadAsync<int>(Prefix.DM, "14000", 1200));
    }

    [TestMethod]
    public void TestBatchBitSplit()
    {
        using var kcpx = Connect();

        // 1500 点 → 1000 + 500 に分割され、2 回目は MR51308（チャンネル繰上げ）から始まる
        var values = RandomValues(1500, () => random.Next(2) == 1);
        kcpx.BatchWrite(Prefix.MR, "50100", values);

        CollectionAssert.AreEqual(values, kcpx.BatchRead<bool>(Prefix.MR, "50100", 1500));
    }

    [TestMethod]
    public void TestRelayAsWords()
    {
        using var kcpx = Connect();

        var words = RandomValues(4, NextUShort);
        kcpx.BatchWrite(Prefix.MR, "52000", words);

        CollectionAssert.AreEqual(words, kcpx.BatchRead<ushort>(Prefix.MR, "52000", 4));

        var bits = kcpx.BatchRead<bool>(Prefix.MR, "52000", 64);
        for (int w = 0; w < words.Length; w++)
        {
            for (int b = 0; b < 16; b++)
            {
                Assert.AreEqual((words[w] >> b & 1) == 1, bits[w * 16 + b], $"MR{520 + w}{b:D2}");
            }
        }
    }

    // ---------------------------------------------------------------
    // 強制セット／リセット（MR53000〜MR53015）
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestForceSetReset()
    {
        using var kcpx = Connect();

        kcpx.ForceSet(Prefix.MR, "53000");
        Assert.IsTrue(kcpx.Read<bool>(Prefix.MR, "53000"));
        await kcpx.ForceResetAsync(Prefix.MR, "53000");
        Assert.IsFalse(kcpx.Read<bool>(Prefix.MR, "53000"));

        kcpx.ContinuousForceSet(Prefix.MR, "53000", 16);
        Assert.AreEqual((ushort)0xFFFF, kcpx.Read<ushort>(Prefix.MR, "53000"));
        await kcpx.ContinuousForceResetAsync(Prefix.MR, "53004", 8);
        Assert.AreEqual((ushort)0xF00F, kcpx.Read<ushort>(Prefix.MR, "53000"));
        kcpx.BatchForceReset(Prefix.MR, "53000", 16);
        Assert.AreEqual((ushort)0, kcpx.Read<ushort>(Prefix.MR, "53000"));
    }

    // ---------------------------------------------------------------
    // タイマ／カウンタ（ラダーで使用している番号のみアクセスできる）
    // ---------------------------------------------------------------

    /// <summary>
    /// テストに使うタイマ番号。ラダーで使用している T を <c>KCPX_PLC_TIMER</c> で指定する（既定 0）。
    /// </summary>
    private static string TimerAddress => GetEnvironmentInt("KCPX_PLC_TIMER", 0).ToString();

    /// <summary>
    /// テストに使うカウンタ番号。ラダーで使用している C を <c>KCPX_PLC_COUNTER</c> で指定する（既定 0）。
    /// </summary>
    private static int CounterNumber => GetEnvironmentInt("KCPX_PLC_COUNTER", 0);

    /// <summary>
    /// タイマの起動条件の MR。ON の間タイマが減算する回路を <c>KCPX_PLC_TIMER_INPUT</c> で指定する（既定 MR002）。
    /// </summary>
    private static string TimerInput => Environment.GetEnvironmentVariable("KCPX_PLC_TIMER_INPUT") ?? "002";

    /// <summary>
    /// カウンタのカウント入力の MR。立ち上がりでカウンタが加算する回路を <c>KCPX_PLC_COUNTER_INPUT</c> で指定する（既定 MR000）。
    /// </summary>
    private static string CounterInput => Environment.GetEnvironmentVariable("KCPX_PLC_COUNTER_INPUT") ?? "000";

    /// <summary>
    /// 条件を満たすまで最大 <paramref name="timeoutMilliseconds"/> の間ポーリングする。
    /// </summary>
    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, int timeoutMilliseconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(50);
        }
        return await condition();
    }

    /// <summary>
    /// ラダーで未使用の T/C は E0 になるため、その場合は <see cref="Assert.Inconclusive(string)"/> で中断する。
    /// </summary>
    private static async Task RunWithTimerCounter(Prefix prefix, string address, Func<Task> test)
    {
        try
        {
            await test();
        }
        catch (HostLinkException ex) when (ex.ErrorCode == HostLinkErrorCode.E0)
        {
            Assert.Inconclusive($"{prefix}{address} にアクセスできませんでした。ラダーで使用しているタイマ／カウンタを KCPX_PLC_TIMER / KCPX_PLC_COUNTER で指定してください。");
        }
    }

    [TestMethod]
    public async Task TestTimerSetValue()
    {
        using var kcpx = Connect();

        await RunWithTimerCounter(Prefix.T, TimerAddress, async () =>
        {
            var original = kcpx.ReadTimerCounter(Prefix.T, TimerAddress).Set;
            uint set = (uint)random.Next(1, 100000);
            try
            {
                kcpx.WriteSetValue(Prefix.T, TimerAddress, set);

                Assert.AreEqual(set, kcpx.ReadTimerCounter(Prefix.T, TimerAddress).Set);
                Assert.AreEqual(set, await kcpx.ReadAsync<uint>(Prefix.TS, TimerAddress));
            }
            finally
            {
                kcpx.WriteSetValue(Prefix.T, TimerAddress, original);
            }
        });
    }

    [TestMethod]
    public async Task TestCounterSetAndCurrentValue()
    {
        using var kcpx = Connect();
        var address = CounterNumber.ToString();

        await RunWithTimerCounter(Prefix.C, address, async () =>
        {
            var original = kcpx.ReadTimerCounter(Prefix.C, address);
            uint set = (uint)random.Next(1, 100000);
            uint current = (uint)random.Next(0, (int)set);
            try
            {
                await kcpx.BatchWriteSetValueAsync(Prefix.C, address, [set]);
                await kcpx.WriteAsync(Prefix.C, address, current);

                var value = (await kcpx.BatchReadTimerCounterAsync(Prefix.C, address, 1))[0];
                Assert.AreEqual(set, value.Set);
                Assert.AreEqual(current, value.Current);
                Assert.AreEqual(set, kcpx.Read<uint>(Prefix.CS, address));
                Assert.AreEqual(current, kcpx.Read<uint>(Prefix.CC, address));
            }
            finally
            {
                kcpx.WriteSetValue(Prefix.C, address, original.Set);
                kcpx.Write(Prefix.C, address, original.Current);
            }
        });
    }

    /// <summary>
    /// カウント入力の立ち上がりで現在値が加算され、設定値に達すると接点が ON になること（ラダーの動作確認）。
    /// </summary>
    [TestMethod]
    public async Task TestCounterCountUp()
    {
        using var kcpx = Connect();
        var address = CounterNumber.ToString();

        await RunWithTimerCounter(Prefix.C, address, async () =>
        {
            var original = kcpx.ReadTimerCounter(Prefix.C, address);
            try
            {
                kcpx.ForceReset(Prefix.MR, CounterInput);
                kcpx.Write(Prefix.C, address, 0u);

                bool contact = true;
                uint current = uint.MaxValue;
                var session = SupportsMonitor
                    ? kcpx.MonitorRegist(m => m
                        .Add<bool>(Prefix.C, address, v => contact = v)
                        .Add<uint>(Prefix.CC, address, v => current = v))
                    : null;

                for (uint n = 1; n <= original.Set; n++)
                {
                    await kcpx.ForceSetAsync(Prefix.MR, CounterInput);
                    Assert.IsTrue(await WaitUntilAsync(async () => (await kcpx.ReadTimerCounterAsync(Prefix.C, address)).Current == n, 1000), $"count {n}");
                    await kcpx.ForceResetAsync(Prefix.MR, CounterInput);
                    // 次の立ち上がりを検出させるため、OFF をスキャンに取り込ませる
                    await Task.Delay(100);

                    if (session != null)
                    {
                        await session.ReadAsync();
                        Assert.AreEqual(n, current);
                        Assert.AreEqual(n >= original.Set, contact, $"contact at count {n}");
                    }
                }

                Assert.IsTrue(kcpx.Read<bool>(Prefix.C, address));
            }
            finally
            {
                kcpx.ForceReset(Prefix.MR, CounterInput);
                kcpx.Write(Prefix.C, address, original.Current);
            }
        });
    }

    /// <summary>
    /// 起動条件の ON で現在値が設定値から減算され、0 で接点が ON、OFF で設定値に戻ること（ラダーの動作確認）。
    /// </summary>
    [TestMethod]
    public async Task TestTimerCountDown()
    {
        using var kcpx = Connect();

        await RunWithTimerCounter(Prefix.T, TimerAddress, async () =>
        {
            try
            {
                kcpx.ForceReset(Prefix.MR, TimerInput);
                var idle = kcpx.ReadTimerCounter(Prefix.T, TimerAddress);
                Assert.IsFalse(idle.Contact);
                Assert.AreEqual(idle.Set, idle.Current);

                await kcpx.ForceSetAsync(Prefix.MR, TimerInput);
                Assert.IsTrue(await WaitUntilAsync(async () => (await kcpx.ReadTimerCounterAsync(Prefix.T, TimerAddress)).Current < idle.Set, 1000), "The timer did not start.");
                Assert.IsTrue(await WaitUntilAsync(async () => await kcpx.ReadAsync<bool>(Prefix.T, TimerAddress), (int)idle.Set * 100 + 2000), "The timer did not time up.");
                Assert.AreEqual(0u, kcpx.ReadTimerCounter(Prefix.T, TimerAddress).Current);

                await kcpx.ForceResetAsync(Prefix.MR, TimerInput);
                Assert.IsTrue(await WaitUntilAsync(async () => !await kcpx.ReadAsync<bool>(Prefix.T, TimerAddress), 1000));
                Assert.AreEqual(idle.Set, kcpx.ReadTimerCounter(Prefix.T, TimerAddress).Current);
            }
            finally
            {
                kcpx.ForceReset(Prefix.MR, TimerInput);
            }
        });
    }

    // ---------------------------------------------------------------
    // 統合ビルダー／モニタ（DM17100〜DM17199 / MR50010〜MR50015）
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task TestCombinedBuilderAsync()
    {
        using var kcpx = Connect();

        var block = RandomValues(50, NextShort);
        int i = NextInt();
        double d = random.NextDouble();
        bool bit = random.Next(2) == 1;

        await kcpx.WriteAsync(b => b
            .Add(Prefix.DM, "17100", block)
            .Add(Prefix.DM, "17150", i)
            .Add(Prefix.DM, "17160", d)
            .Add(Prefix.MR, "50010", bit));

        short[] rblock = []; int ri = 0; double rd = 0; bool rbit = !bit;
        await kcpx.ReadAsync(b => b
            .Add<short>(Prefix.DM, "17100", 50, v => rblock = v)
            .Add<int>(Prefix.DM, "17150", v => ri = v)
            .Add<double>(Prefix.DM, "17160", v => rd = v)
            .Add<bool>(Prefix.MR, "50010", v => rbit = v));

        CollectionAssert.AreEqual(block, rblock);
        Assert.AreEqual(i, ri);
        Assert.AreEqual(d, rd);
        Assert.AreEqual(bit, rbit);
    }

    [TestMethod]
    public void TestMonitor()
    {
        using var kcpx = Connect();
        if (!SupportsMonitor)
        {
            Assert.ThrowsException<NotSupportedException>(() => kcpx.MonitorRegist(b => b.Add<short>(Prefix.DM, "17180", _ => { })));
            return;
        }

        short ms = 0; int mi = 0; double md = 0; ushort mw = 0; bool mbit = false;
        var session = kcpx.MonitorRegist(b => b
            .Add<short>(Prefix.DM, "17180", v => ms = v)
            .Add<int>(Prefix.DM, "17182", v => mi = v)
            .Add<double>(Prefix.DM, "17184", v => md = v)
            .Add<ushort>(Prefix.MR, "52100", v => mw = v)
            .Add<bool>(Prefix.MR, "50015", v => mbit = v));

        // 登録後に値を変えても、同じセッションで最新値が読めること
        for (int n = 0; n < 3; n++)
        {
            short s = NextShort();
            int i = NextInt();
            double d = random.NextDouble();
            ushort w = NextUShort();
            bool bit = n % 2 == 0;

            kcpx.Write(Prefix.DM, "17180", s);
            kcpx.Write(Prefix.DM, "17182", i);
            kcpx.Write(Prefix.DM, "17184", d);
            kcpx.Write(Prefix.MR, "52100", w);
            kcpx.Write(Prefix.MR, "50015", bit);

            session.Read();

            Assert.AreEqual(s, ms);
            Assert.AreEqual(i, mi);
            Assert.AreEqual(d, md);
            Assert.AreEqual(w, mw);
            Assert.AreEqual(bit, mbit);
        }
    }

    // ---------------------------------------------------------------
    // 動作モード
    // ---------------------------------------------------------------

    /// <summary>
    /// PROGRAM モードに切り替えて確認し、RUN モードに戻すこと。
    /// </summary>
    [TestMethod]
    public async Task TestOperatingMode()
    {
        using var kcpx = Connect();
        if (ModeChangeEnableVar != null)
        {
            EnsureEnabled(ModeChangeEnableVar);
        }
        if (kcpx.ReadOperatingMode() != OperatingMode.Run)
        {
            Assert.Inconclusive("RUN モードの状態で実行してください。");
        }

        try
        {
            kcpx.ChangeOperatingMode(OperatingMode.Program);
            Assert.AreEqual(OperatingMode.Program, await kcpx.ReadOperatingModeAsync());

            // M1 は OK の応答後、RUN へ移行するまで時間がかかる場合がある（KV-7500 で約 25 ms）
            await kcpx.ChangeOperatingModeAsync(OperatingMode.Run);
            Assert.IsTrue(await WaitUntilAsync(async () => await kcpx.ReadOperatingModeAsync() == OperatingMode.Run, 1000));

            await kcpx.ChangeOperatingModeAsync(OperatingMode.Program);
            Assert.AreEqual(OperatingMode.Program, kcpx.ReadOperatingMode());
        }
        finally
        {
            kcpx.ChangeOperatingMode(OperatingMode.Run);
        }
        Assert.IsTrue(await WaitUntilAsync(async () => await kcpx.ReadOperatingModeAsync() == OperatingMode.Run, 1000));
    }

    // ---------------------------------------------------------------
    // エラーレスポンス
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestDeviceNumberOutOfRange()
    {
        using var kcpx = Connect();

        var ex = Assert.ThrowsException<HostLinkException>(() => kcpx.Read<ushort>(Prefix.DM, "65535"));
        Assert.AreEqual(HostLinkErrorCode.E0, ex.ErrorCode);

        // エラー後も同じ接続で通信を継続できること
        kcpx.Write(Prefix.DM, "17090", (ushort)1234);
        Assert.AreEqual((ushort)1234, kcpx.Read<ushort>(Prefix.DM, "17090"));
    }
}
