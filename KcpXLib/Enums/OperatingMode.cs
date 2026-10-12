namespace KcpXLib.Enums;

/// <summary>
/// PLC の動作モード
/// </summary>
public enum OperatingMode
{
    /// <summary>PROGRAM モード（ラダー未登録の場合を含む）</summary>
    Program = 0,
    /// <summary>RUN モード</summary>
    Run = 1,
}
