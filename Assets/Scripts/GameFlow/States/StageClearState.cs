using UnityEngine;

/// <summary>
/// 過關後的緩衝 State。
/// Enter() → GenerateNextOptions → 等 OnRoomCleared（門或出口都用同一個事件）→ 推進下一關。
/// SelectOption 由 DoorController.TrySelect() 在觸發 OnRoomCleared 之前呼叫，
/// 此時 PendingOptions 已在 Enter() 填好，時序正確。
/// </summary>
public class StageClearState : IState
{
    private int roomNumber;

    public StageClearState(int roomNumber) => this.roomNumber = roomNumber;

    public void Enter()
    {
        Debug.Log($"[StageClearState] Enter - 房號: {roomNumber}");
        MissionTracker.Reset();

        // 重新顯示 HUD：GameplayState.Exit() 不再提前隱藏，但若 UIManager 狀態不一致時作為保險
        // 主要目的是確保 ExitPrompt（含 HUD）在等玩家走門期間維持可見
        UIManager.Instance?.ShowGameplayHUD();

        var campaign = GameDB.Instance?.Campaign;
        campaign?.EnterNextRoom();

        // Demo 固定 8 關，第 4、8 關為 Boss（依 TotalRoomNumber 判斷，與 block 系統無關）
        if (campaign != null && campaign.IsNextRoomBoss())
        {
            Debug.Log($"[StageClearState] 第 {campaign.TotalRoomNumber + 1} 關為 Boss 戰，準備進入！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
            return;
        }

        // 立即生成岔路選項，確保玩家走門時 PendingOptions 已有資料
        var pool = GameDB.Instance?.MissionPool;
        if (pool != null)
        {
            var drawn = pool.DrawRandom(2);
            campaign?.GenerateNextOptions(drawn[0], drawn[1]);
            Debug.Log($"[StageClearState] PendingOptions 已生成：{drawn[0]?.name ?? "null"} / {drawn[1]?.name ?? "null"}");
        }
        else
        {
            campaign?.GenerateNextOptions();
            Debug.LogWarning("[StageClearState] MissionPool 未設定，使用 fallback");
        }

        BattleEventManager.OnRoomCleared += HandleRoomCleared;
    }

    private void HandleRoomCleared()
    {
        // ActiveRoom 已由 DoorController.SelectOption（或出口路徑不需要選項）寫入
        Debug.Log($"[StageClearState] 過關，前往下一關: {roomNumber + 1}");
        GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber + 1));
    }

    public void Exit()
    {
        BattleEventManager.OnRoomCleared -= HandleRoomCleared;
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
