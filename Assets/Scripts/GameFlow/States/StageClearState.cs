using UnityEngine;
using System;

public class StageClearState : IState
{
    private int roomNumber;

    // 記錄本關任務是否達成，Enter 時從 MissionTracker 讀取一次性結果
    private bool _missionCompleted = false;

    public StageClearState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[StageClearState] Enter - 小結算序列開始，房號: {roomNumber}");
        Time.timeScale = 0f;

        // 讀取本關任務達成狀態（MissionTracker 在 OnDisable 前已結算）
        // 若場景沒有 MissionTracker（無任務），預設視為達成，走原本獎勵流程
        _missionCompleted = MissionTracker.Instance == null || MissionTracker.LastResult;

        // 任務達成 → 設定本次結算的獎勵過濾器
        if (_missionCompleted)
            ApplyMissionRewardFilter();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideExitPrompt();
            UIManager.Instance.OnPolicyCardSelected += HandlePolicyCardSelected;
            UIManager.Instance.StartStageClearSequence(roomNumber, OnSequenceFinished);
        }
        else
        {
            OnSequenceFinished();
        }
    }

    private void HandlePolicyCardSelected(PolicyCardData card)
    {
        Debug.Log($"[StageClearState] 選擇了政策卡: {card.cardName}");
        GameDB.Instance?.Run.AddPolicyCard(card);
    }

    private void OnSequenceFinished()
    {
        var campaign = GameDB.Instance?.Campaign;

        campaign?.EnterNextRoom();

        // 最後一關 → Boss 戰（不需要岔路選項）
        if (campaign != null && campaign.IsLastRoomInBlock())
        {
            Debug.Log("[StageClearState] 序列結束，準備進入 Boss 戰！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
            return;
        }

        // 非最後一關 → 從任務池抽兩個選項注入岔路
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
            Debug.Log($"[StageClearState] 序列結束，隨機命中！前往安全房: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new SafeRoomState(roomNumber + 1));
        }
        else
        {
            Debug.Log($"[StageClearState] 序列結束，前往下一關戰鬥: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber + 1));
        }
    }

    public void Exit()
    {
        Debug.Log($"[StageClearState] Exit - 離開小結算，房號: {roomNumber}");
        Time.timeScale = 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPolicyCardSelected -= HandlePolicyCardSelected;
            UIManager.Instance.HideStageClearPanel();
        }
    }

    public void Update() { }
    public void PhysicsUpdate() { }

    // ── 內部 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 根據本關任務類型，設定本次結算的獎勵卡牌過濾器。
    /// 找不到設定時 fallback 至不限制（原本的隨機抽牌邏輯）。
    /// </summary>
    private void ApplyMissionRewardFilter()
    {
        var mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        var config  = GameDB.Instance?.missionRewardConfig;

        if (mission == null || config == null) return;

        CardType[] allowed = config.GetAllowedCardTypes(mission.objectiveType);
        if (allowed != null && allowed.Length > 0)
        {
            UIManager.Instance?.SetRewardFilter(allowed);
            Debug.Log($"[StageClearState] 套用任務獎勵過濾器：{mission.objectiveType}");
        }
    }
}
