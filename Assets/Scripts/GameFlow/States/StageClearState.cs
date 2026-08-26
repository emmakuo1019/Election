using UnityEngine;

/// <summary>
/// 過關後的緩衝 State。
/// 目前職責：推進房間進度、從任務池生成岔路選項、切換到下一個 State。
/// 之後在此加入漫畫網點轉場動畫。
/// 選卡與技能選擇已改為場景內互動，由 RewardItemSpawner 負責。
/// </summary>
public class StageClearState : IState
{
    private int roomNumber;

    public StageClearState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[StageClearState] Enter - 房號: {roomNumber}");

        // ponytail: 轉場動畫預留點，之後在此插入漫畫網點過場
        Proceed();
    }

    private void Proceed()
    {
        var campaign = GameDB.Instance?.Campaign;

        campaign?.EnterNextRoom();

        // 最後一關 → Boss 戰
        if (campaign != null && campaign.IsLastRoomInBlock())
        {
            Debug.Log("[StageClearState] 準備進入 Boss 戰！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
            return;
        }

        // 從任務池抽兩個選項注入岔路
        var pool = GameDB.Instance?.missionPool;
        if (pool != null)
        {
            var drawn = pool.DrawRandom(2);
            campaign?.GenerateNextOptions(drawn[0], drawn[1]);
        }
        else
        {
            // ponytail: 無任務池時 fallback，門口顯示「前往下一區域」
            campaign?.GenerateNextOptions();
        }

        if (UnityEngine.Random.value <= GameFlowManager.Instance.SafeRoomSpawnChance)
        {
            Debug.Log($"[StageClearState] 前往安全房: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new SafeRoomState(roomNumber + 1));
        }
        else
        {
            Debug.Log($"[StageClearState] 前往下一關戰鬥: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber + 1));
        }
    }

    public void Exit()
    {
        Debug.Log($"[StageClearState] Exit - 房號: {roomNumber}");
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
