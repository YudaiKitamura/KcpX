namespace KcpXLib.Enums;

/// <summary>
/// 上位リンクのエラーレスポンス
/// </summary>
public enum HostLinkErrorCode
{
    /// <summary>デバイス番号異常（範囲外、未使用のタイマ・カウンタ、モニタ未登録での読出し など）</summary>
    E0 = 0,
    /// <summary>コマンド異常（未対応のコマンド、書式の誤り）</summary>
    E1 = 1,
    /// <summary>プログラム未登録</summary>
    E2 = 2,
    /// <summary>書込み禁止（書込みプロテクト中の設定値変更）</summary>
    E4 = 4,
    /// <summary>本体エラー（CPU エラー未解除）</summary>
    E5 = 5,
    /// <summary>コメントなし</summary>
    E6 = 6,
}
