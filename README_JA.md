# KcpX
<p>
  <img alt="Version" src="https://img.shields.io/badge/version-0.1.0-blue" />
  <img alt=".NET 7.0+" src="https://img.shields.io/badge/.NET-7.0+-blueviolet" />
  <img alt=".NET 8.0+" src="https://img.shields.io/badge/.NET-8.0+-purple" />
  <img alt=".NET 9.0+" src="https://img.shields.io/badge/.NET-9.0+-indigo" />
  <img alt=".NET Core 2.0+" src="https://img.shields.io/badge/.NET_Core-2.0+-darkgreen" />
  <img alt=".NET Framework 4.6.1+" src="https://img.shields.io/badge/.NET_Framework-4.6.1+-teal?logo=windows" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg" />
</p>

<p>
  <a href="README_JA.md">日本語</a> | <a href="README.md">English</a>
</p>

**KcpX は、キーエンス KV シリーズ PLC と上位リンク通信を行うためのライブラリです。**  
[McpX](https://github.com/YudaiKitamura/McpX) と同じシンプルな API で、上位リンクの仕様を意識せずに通信できます。  
Linux、Windows、macOS など、様々なプラットフォームで動作します。

## インストール
### .NET CLI
```sh
dotnet add package KcpX
```
### パッケージマネージャー（Visual Studio）
```sh
PM> NuGet\Install-Package KcpX
```

## 使用例
```csharp
using KcpXLib;
using KcpXLib.Enums;

// PLC に接続（上位リンクのポート 8501、TCP）
using (var kcpx = new KcpX("192.168.12.89", 8501, series: PlcSeries.KV7000))
{
    // MR0 から 3000 点を読み出し（1000 点ずつ自動分割）
    bool[] mrArr = kcpx.BatchRead<bool>(Prefix.MR, "0", 3000);

    // DM1000 から 2000 ワードを読み出し
    short[] dmArr = kcpx.BatchRead<short>(Prefix.DM, "1000", 2000);

    // DM0〜DM1 に 1234、DM2〜DM3 に 5678 を符号付き 32 ビット整数で書込み
    kcpx.BatchWrite<int>(Prefix.DM, "0", [1234, 5678]);

    // 範囲と単一デバイスをまとめて読み出し
    short[] block = [];
    int total = 0;
    kcpx.Read(b => b
        .Add<short>(Prefix.DM, "400", 100, v => block = v)
        .Add<int>(Prefix.DM, "2000", v => total = v));

    // タイマの接点・現在値・設定値
    TimerCounterValue t0 = kcpx.ReadTimerCounter(Prefix.T, "0");
}
```
[C# のサンプルはこちら](https://github.com/YudaiKitamura/KcpX/tree/main/Example)

### KV STUDIO シミュレータへの接続
`KcpXSimulator` を使うと、KV STUDIO のシミュレータに上位リンク（TCP）で接続できます。（KV STUDIO Ver.12.41 で確認）

```csharp
using var sim = new KcpXSimulator();
using var remote = new KcpXSimulator("192.168.12.90");  // 別の PC で動作するシミュレータ
```

- 対象 PLC は KV-8000 / KV-X500 のプロジェクトです。
- 接続できるシミュレータは 1 台のみです（複数起動した場合は、最初に起動したシミュレータに接続されます）。
- シミュレータは 127.0.0.1 でのみ待ち受けます。別の PC から接続する場合は、シミュレータ側の PC で 8501 を 127.0.0.1 へ転送してください（`netsh interface portproxy` など）。
- 交信は TCP のみです。モニタ（MBS / MBR / MWS / MWR）には対応していません（`NotSupportedException`）。

## 対応コマンド

| 名称 | 説明 | 上位リンクコマンド | 同期メソッド | 非同期メソッド |
|---|---|---|---|---|
| **単一読出し** | 指定したデバイスの値を読み出します。 | RD / RDS | `Read<T>(Prefix prefix, string address)` | `ReadAsync<T>(...)` |
| **単一書込み** | 指定したデバイスに値を書き込みます。 | WR / WRS | `Write<T>(Prefix prefix, string address, T value)` | `WriteAsync<T>(...)` |
| **一括読出し** | 連続したデバイスを読み出します。点数の上限を超える場合は自動で分割します。 | RDS | `BatchRead<T>(Prefix prefix, string address, ushort length)` | `BatchReadAsync<T>(...)` |
| **一括書込み** | 連続したデバイスに配列を書き込みます。点数の上限を超える場合は自動で分割します。 | WRS | `BatchWrite<T>(Prefix prefix, string address, T[] values)` | `BatchWriteAsync<T>(...)` |
| **統合読出し / 書込み** | 範囲と単一デバイスをまとめて読み書きします。 | RD / RDS / WR / WRS | `Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` | `ReadAsync(...)` / `WriteAsync(...)` |
| **タイマ / カウンタ読出し** | T / C の接点・現在値・設定値を読み出します。 | RD / RDS | `ReadTimerCounter(Prefix prefix, string address)` / `BatchReadTimerCounter(...)` | `ReadTimerCounterAsync(...)` / `BatchReadTimerCounterAsync(...)` |
| **設定値書込み** | T / C / CTC の設定値を書き込みます。 | WS / WSS | `WriteSetValue<T>(...)` / `BatchWriteSetValue<T>(...)` | `WriteSetValueAsync<T>(...)` / `BatchWriteSetValueAsync<T>(...)` |
| **強制セット / リセット** | ビットデバイスを強制的に ON / OFF します。 | ST / RS | `ForceSet(Prefix prefix, string address)` / `ForceReset(...)` | `ForceSetAsync(...)` / `ForceResetAsync(...)` |
| **連続強制セット / リセット** | 連続したビットデバイスを強制的に ON / OFF します（16 点ごとに分割）。 | STS / RSS | `BatchForceSet(Prefix prefix, string address, ushort count)` / `BatchForceReset(...)` | `BatchForceSetAsync(...)` / `BatchForceResetAsync(...)` |
| **モニタ登録** | ビット・ワードそれぞれ最大 120 点を登録し、`MonitorSession` を返します。 | MBS / MWS | `MonitorRegist(Action<MonitorBuilder>)` | `MonitorRegistAsync(...)` |
| **モニタ読出し** | 登録したデバイスの最新値を読み出します。 | MBR / MWR | `MonitorSession.Read()` | `MonitorSession.ReadAsync()` |
| **動作モード確認** | PLC の動作モード（PROGRAM / RUN）を読み出します。 | ?M | `ReadOperatingMode()` | `ReadOperatingModeAsync()` |
| **モード切替** | PLC を PROGRAM / RUN モードに切り替えます。 | M0 / M1 | `ChangeOperatingMode(OperatingMode mode)` | `ChangeOperatingModeAsync(...)` |

`ReadInt16`、`BatchReadDouble`、`WriteUInt32Async` などの型別メソッドも使用できます。

### 対応する型
| 型 | データ形式 | 備考 |
|---|---|---|
| `bool` | ビット | ビットデバイス、および T / C / CTC の接点 |
| `ushort` / `short` | `.U` / `.S` | |
| `uint` / `int` / `float` | `.D` / `.L` / `.D` | 指定したデバイスが下位ワードです。上位・下位ワードの同時性はデバイス番号が偶数の場合のみ保証されます。 |
| `ulong` / `long` / `double` | `.D` × 2 | |

## 対応デバイス
R, B, MR, LR, CR, VB, DM, EM, FM, ZF, W, TM, Z, T, TC, TS, C, CC, CS, CTH, CTC, AT, CM, VM

- R, MR, LR, CR はチャンネル + 2 桁のビット番号で指定します（例: `"1015"` = チャンネル 10 のビット 15）。B, VB, W は 16 進数です。
- ラダープログラムで使用していない T / C の番号はエラー `E0` になります。

## 対応プロトコル
- TCP
- UDP

## 変更履歴
- [CHANGELOG_JA.md](./CHANGELOG_JA.md)

## 関連
- [McpX](https://github.com/YudaiKitamura/McpX) 三菱電機製 PLC と MC プロトコルで通信するためのライブラリ
