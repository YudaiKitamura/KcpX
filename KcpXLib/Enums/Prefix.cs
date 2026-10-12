namespace KcpXLib.Enums;

/// <summary>
/// KV シリーズのデバイスの接頭辞（例: <c>DM100</c> の <c>DM</c>）
/// </summary>
public enum Prefix
{
    /// <summary>リレー（チャンネル + ビット番号 00〜15）</summary>
    R,
    /// <summary>リンクリレー（16進数）</summary>
    B,
    /// <summary>内部補助リレー（チャンネル + ビット番号 00〜15）</summary>
    MR,
    /// <summary>ラッチリレー（チャンネル + ビット番号 00〜15）</summary>
    LR,
    /// <summary>コントロールリレー（チャンネル + ビット番号 00〜15）</summary>
    CR,
    /// <summary>ワークリレー（16進数）</summary>
    VB,
    /// <summary>データメモリ</summary>
    DM,
    /// <summary>拡張データメモリ</summary>
    EM,
    /// <summary>ファイルレジスタ</summary>
    FM,
    /// <summary>ファイルレジスタ（連番）</summary>
    ZF,
    /// <summary>リンクレジスタ（16進数）</summary>
    W,
    /// <summary>テンポラリデータメモリ</summary>
    TM,
    /// <summary>インデックスレジスタ</summary>
    Z,
    /// <summary>タイマ（接点・現在値・設定値）</summary>
    T,
    /// <summary>タイマ（現在値）</summary>
    TC,
    /// <summary>タイマ（設定値）</summary>
    TS,
    /// <summary>カウンタ（接点・現在値・設定値）</summary>
    C,
    /// <summary>カウンタ（現在値）</summary>
    CC,
    /// <summary>カウンタ（設定値）</summary>
    CS,
    /// <summary>高速カウンタ</summary>
    CTH,
    /// <summary>高速カウンタコンパレータ</summary>
    CTC,
    /// <summary>デジタルトリマ</summary>
    AT,
    /// <summary>コントロールメモリ</summary>
    CM,
    /// <summary>ワークメモリ</summary>
    VM,
}
