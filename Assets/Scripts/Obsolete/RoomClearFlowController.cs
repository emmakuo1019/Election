using UnityEngine;

/// <summary>
/// 僅保留給既有場景序列化引用的相容殼。正式戰役不會呼叫這個元件；
/// 任務結果、獎勵與路線一律由 StageClearState 處理。
/// </summary>
[System.Obsolete("正式戰役已由 StageClearState 接管；請從場景移除此相容元件。", false)]
public class RoomClearFlowController : MonoBehaviour
{
    public void OnRoomCleared(bool _) { }
    public bool HasPendingSettlement() => false;
    public void ShowSettlementAtExit() { }
    public void OnContinuePressed() { }
}
