using PadLab.Core.Model;

namespace PadLab.Core.Analysis;

/// <summary>
/// 入力を1サンプルずつ受け取って状態を更新する。Feed は解析スレッドから、
/// 結果プロパティは UI スレッドから読まれるため、実装は lock で守り結果は不変オブジェクトで返す。
/// </summary>
public interface IAnalyzer
{
    void Feed(GamepadSnapshot snapshot);
    void Reset();
}

/// <summary>3段階の参考判定。断定表現は使わない。</summary>
public enum Verdict { None, Minor, Notable }
