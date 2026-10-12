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

**KcpX is a library for communicating with KEYENCE KV series PLCs using the host link protocol (上位リンク).**  
It has the same simple API as [McpX](https://github.com/YudaiKitamura/McpX), so you can communicate without worrying about host link details.  
It runs on various platforms, including Linux, Windows, and macOS.

## Installation
### .NET CLI
```sh
dotnet add package KcpX
```
### Package Manager(Visual Studio)
```sh
PM> NuGet\Install-Package KcpX
```

## Example Usage
```csharp
using KcpXLib;
using KcpXLib.Enums;

// Connect to the PLC (host link port 8501, TCP)
using (var kcpx = new KcpX("192.168.12.89", 8501, series: PlcSeries.KV7000))
{
    // Read 3000 points starting from MR0 (split into requests of up to 1000 points)
    bool[] mrArr = kcpx.BatchRead<bool>(Prefix.MR, "0", 3000);

    // Read 2000 words starting from DM1000
    short[] dmArr = kcpx.BatchRead<short>(Prefix.DM, "1000", 2000);

    // Write 1234 to DM0-DM1 and 5678 to DM2-DM3 as signed 32-bit integers
    kcpx.BatchWrite<int>(Prefix.DM, "0", [1234, 5678]);

    // Read ranges and single devices together
    short[] block = [];
    int total = 0;
    kcpx.Read(b => b
        .Add<short>(Prefix.DM, "400", 100, v => block = v)
        .Add<int>(Prefix.DM, "2000", v => total = v));

    // Timer contact, current value and set value
    TimerCounterValue t0 = kcpx.ReadTimerCounter(Prefix.T, "0");
}
```
[C# samples are available here.](https://github.com/YudaiKitamura/KcpX/tree/main/Example)

### Connecting to the KV STUDIO simulator
`KcpXSimulator` connects to the KV STUDIO simulator over host link (TCP). (Verified with KV STUDIO Ver.12.41)

```csharp
using var sim = new KcpXSimulator();
using var remote = new KcpXSimulator("192.168.12.90");  // Simulator running on another PC
```

- Target PLCs are KV-8000 / KV-X500 projects.
- Only one simulator can be connected (when several are running, the first one started is connected).
- The simulator listens on 127.0.0.1 only. To connect from another PC, forward port 8501 to 127.0.0.1 on the simulator PC (e.g. `netsh interface portproxy`).
- TCP only. Monitor commands (MBS / MBR / MWS / MWR) are not supported (`NotSupportedException`).

## Supported Commands

| Name | Description | Host link command | Synchronous Method | Asynchronous Method |
|---|---|---|---|---|
| **Single Read** | Reads a single value from the specified device. | RD / RDS | `Read<T>(Prefix prefix, string address)` | `ReadAsync<T>(...)` |
| **Single Write** | Writes a single value to the specified device. | WR / WRS | `Write<T>(Prefix prefix, string address, T value)` | `WriteAsync<T>(...)` |
| **Batch Read** | Reads consecutive values. Requests exceeding the point limit are split automatically. | RDS | `BatchRead<T>(Prefix prefix, string address, ushort length)` | `BatchReadAsync<T>(...)` |
| **Batch Write** | Writes an array of values to consecutive devices. Requests exceeding the point limit are split automatically. | WRS | `BatchWrite<T>(Prefix prefix, string address, T[] values)` | `BatchWriteAsync<T>(...)` |
| **Combined Read / Write** | Reads or writes ranges and single devices together. | RD / RDS / WR / WRS | `Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` | `ReadAsync(...)` / `WriteAsync(...)` |
| **Timer / Counter Read** | Reads the contact, current value and set value of T / C. | RD / RDS | `ReadTimerCounter(Prefix prefix, string address)` / `BatchReadTimerCounter(...)` | `ReadTimerCounterAsync(...)` / `BatchReadTimerCounterAsync(...)` |
| **Set Value Write** | Writes the set value of T / C / CTC. | WS / WSS | `WriteSetValue<T>(...)` / `BatchWriteSetValue<T>(...)` | `WriteSetValueAsync<T>(...)` / `BatchWriteSetValueAsync<T>(...)` |
| **Force Set / Reset** | Forces a bit device ON / OFF. | ST / RS | `ForceSet(Prefix prefix, string address)` / `ForceReset(...)` | `ForceSetAsync(...)` / `ForceResetAsync(...)` |
| **Batch Force Set / Reset** | Forces consecutive bit devices ON / OFF (split every 16 points). | STS / RSS | `BatchForceSet(Prefix prefix, string address, ushort count)` / `BatchForceReset(...)` | `BatchForceSetAsync(...)` / `BatchForceResetAsync(...)` |
| **Monitor Registration** | Registers up to 120 bit and 120 word points and returns a `MonitorSession`. | MBS / MWS | `MonitorRegist(Action<MonitorBuilder>)` | `MonitorRegistAsync(...)` |
| **Monitor Read** | Reads the latest values of the registered devices. | MBR / MWR | `MonitorSession.Read()` | `MonitorSession.ReadAsync()` |
| **Operating Mode Read** | Reads the operating mode (PROGRAM / RUN) of the PLC. | ?M | `ReadOperatingMode()` | `ReadOperatingModeAsync()` |
| **Operating Mode Change** | Switches the PLC to PROGRAM / RUN mode. | M0 / M1 | `ChangeOperatingMode(OperatingMode mode)` | `ChangeOperatingModeAsync(...)` |

Typed methods such as `ReadInt16`, `BatchReadDouble` and `WriteUInt32Async` are also available.

### Supported types
| Type | Data format | Note |
|---|---|---|
| `bool` | bit | Bit devices, and the contacts of T / C / CTC |
| `ushort` / `short` | `.U` / `.S` | |
| `uint` / `int` / `float` | `.D` / `.L` / `.D` | The lower word is the specified device. The upper and lower words are read simultaneously only when the device number is even. |
| `ulong` / `long` / `double` | `.D` × 2 | |

## Supported Devices
R, B, MR, LR, CR, VB, DM, EM, FM, ZF, W, TM, Z, T, TC, TS, C, CC, CS, CTH, CTC, AT, CM, VM

- R, MR, LR, CR are specified as channel + 2-digit bit number (e.g. `"1015"` = channel 10, bit 15). B, VB, W are hexadecimal.
- T / C numbers that are not used in the ladder program return error `E0`.

## Supported Protocols
- TCP
- UDP

## Changelog
- [CHANGELOG.md](./CHANGELOG.md)

## Related
- [McpX](https://github.com/YudaiKitamura/McpX) A library for communicating with Mitsubishi Electric PLCs using the MC protocol.
