using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameplayState : IState
{
    private int roomNumber;

    public GameplayState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    private string GetBattleSceneName()
    {
        // 優先從 CampaignData 的房間序列取得場景名稱
        string sceneName = GameDB.Instance?.Campaign?.GetCurrentRoomSceneName();
        if (!string.IsNullOrEmpty(sceneName)) return sceneName;

        // ponytail: 無序列時 fallback 至預設場景，上限是 CampaignData.StartRandomBlock 未被呼叫
        Debug.LogWarning("[GameplayState] CampaignData 無房間序列，使用預設場景 TestMVP");
        return "TestMVP";
    }

    public void Enter()
    {
        Debug.Log($"[GameplayState] Enter - 進入戰鬥房間，房號: {roomNumber}");

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadBattleSceneRoutine());
        }

        BattleEventManager.OnRoomCleared += HandleRoomCleared;
        BattleEventManager.OnPlayerDied += HandlePlayerDied;
    }

    private IEnumerator LoadBattleSceneRoutine()
    {
        string battleSceneName = GetBattleSceneName();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Single);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // 安全檢查：若狀態已經被切走，則中止協程
            if (GameFlowManager.Instance.CurrentState != this) yield break;

            Debug.Log($"[GameplayState] 場景 {battleSceneName} 載入完成！");

            // 步驟 A: UI 先掛載並綁定
            if (UIManager.Instance != null) UIManager.Instance.ShowGameplayHUD();
            
            // 步驟 B: 用 LevelTimer Inspector 上設定的 levelDuration 啟動
            if (LevelTimer.Instance != null) LevelTimer.Instance.StartTimer(LevelTimer.Instance.TotalDuration);

            // 步驟 C: 顯示本關任務簡報（若 ActiveRoom 有指定 mission 且有 briefingStep）
            TutorialStepData briefing = GameDB.Instance?.Campaign.ActiveRoom.mission?.briefingStep;
            if (briefing != null)
                UIManager.Instance?.ShowTutorialDialogue(briefing);
        }
        else
        {
            Debug.LogWarning($"[GameplayState] 找不到場景 {battleSceneName}，請確認是否加入 Build Settings。");
        }
    }

    private void HandleRoomCleared()
    {
        Debug.Log($"[GameplayState] 收到過關事件，準備前往結算 (房號: {roomNumber})");
        GameFlowManager.Instance.ChangeState(new StageClearState(roomNumber));
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[GameplayState] 收到玩家死亡事件，前往遊戲結束狀態");
        GameFlowManager.Instance.ChangeState(new GameEndState(false));
    }
    
    public void Exit()
    {
        Debug.Log($"[GameplayState] Exit - 離開戰鬥房間，房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.HideGameplayHUD();

        // ⚠️ 解除訂閱戰鬥事件，避免 Memory Leak 或重複觸發
        BattleEventManager.OnRoomCleared -= HandleRoomCleared;
        BattleEventManager.OnPlayerDied -= HandlePlayerDied;
    }
    
    public void Update() 
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 按下 Y 鍵：模擬正常過關事件（會走結算 UI 流程，結束後 roomNumber + 1）
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log($"[GameplayState] 偵測到按下 Y 鍵，模擬過關！當前關卡: {roomNumber}");
            BattleEventManager.TriggerRoomCleared();
        }

        // 按下 N 鍵：直接跳過結算 UI，進入下一關 roomNumber + 1 (如果是 14 則進入 Boss 戰)
        if (Input.GetKeyDown(KeyCode.N))
        {
            int nextRoom = roomNumber + 1;
            Debug.Log($"[GameplayState] 偵測到按下 N 鍵，直接跳過結算 UI！關卡切換: {roomNumber} -> {nextRoom}");
            if (roomNumber == 14)
            {
                GameFlowManager.Instance.ChangeState(new BossBattleState());
            }
            else
            {
                GameFlowManager.Instance.ChangeState(new GameplayState(nextRoom));
            }
        }
#endif
    }
    public void PhysicsUpdate() { }
}
