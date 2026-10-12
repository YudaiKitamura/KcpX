namespace KcpXLib;

/// <summary>
/// タイマ・カウンタ（T, C, CTH, CTC）の値
/// </summary>
/// <param name="contact">接点の状態</param>
/// <param name="current">現在値</param>
/// <param name="set">設定値</param>
public readonly struct TimerCounterValue(bool contact, uint current, uint set)
{
    /// <summary>
    /// 接点の状態（ON: <c>true</c>）。CTH は常に <c>false</c> です。
    /// </summary>
    public bool Contact { get; } = contact;

    /// <summary>
    /// 現在値
    /// </summary>
    public uint Current { get; } = current;

    /// <summary>
    /// 設定値。CTH は常に <c>0</c> です。
    /// </summary>
    public uint Set { get; } = set;
}
