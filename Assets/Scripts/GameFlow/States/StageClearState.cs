using UnityEngine;
using System;

public class StageClearState : IState
{
    private int roomNumber;

    public StageClearState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[StageClearState] Enter - 小結算序列開始，房號: {roomNumber}");
        Time.timeScale = 0f; // 暫停時間

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
        
        // 呼叫 PolicyEffectRuntimeManager 發放獎勵
        if (PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.ApplyCard(card);
        }

        // ⚠️ 修正：移除這裡的 OnSequenceFinished()。
        // 將流程控制權交還給 UIManager，讓它繼續處理技能面板等後續流程，
        // 等所有面板都關閉後，UIManager 才會安全地觸發 OnSequenceFinished()。
    }

    private void OnSequenceFinished()
    {
        if (roomNumber == 15)
        {
            Debug.Log("[StageClearState] 序列結束，準備進入 Boss 戰！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
        }
        else
        {
            // 利用 Random.value 取得 0.0 ~ 1.0 的隨機值，若小於等於設定機率則進入安全房
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
    }

    public void Exit()
    {
        Debug.Log($"[StageClearState] Exit - 離開小結算，房號: {roomNumber}");
        Time.timeScale = 1f; // 恢復時間

        // UIManager 會在 Sequence 結束時自行關閉面板，也可以在這裡做二次確保
        if (UIManager.Instance != null) 
        {
            UIManager.Instance.OnPolicyCardSelected -= HandlePolicyCardSelected;
            UIManager.Instance.HideStageClearPanel();
        }
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
