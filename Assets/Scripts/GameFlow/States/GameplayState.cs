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

        // 在載入新場景前才隱藏 HUD（含 Tutorial UI），
        // 避免 StageClearState 等待走門期間畫面空白
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameplayHUD();
            UIManager.Instance.HideAllTutorialUI();
        }

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
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameplayHUD();
                // 場景切換後重新綁定場景內的 Tutorial / Reward UI（舊場景引用在切換後失效）
                UIManager.Instance.RebindTutorialUI();
            }

            // 步驟 B: 啟動計時器
            //   Survive 任務 → 以 mission.targetValue（秒數）作為倒數時間
            //   其他任務     → 沿用 LevelTimer Inspector 上設定的 levelDuration
            if (LevelTimer.Instance != null)
            {
                RoomMissionData mission = GameDB.Instance?.Campaign.ActiveRoom.mission;

                // ── 診斷 log（任何時候都印，方便確認資料是否正確傳入）──
                if (mission == null)
                    Debug.LogWarning("[GameplayState] ⚠️ ActiveRoom.mission 為 null！計時器使用 Inspector 預設值。" +
                                     "請確認 MissionPool 已在 GameDB Inspector 設定，且 RoomMissionData.sceneName 正確。");
                else
                    Debug.Log($"[GameplayState] ✅ 讀到任務：{mission.objectiveType}，targetValue={mission.targetValue}，sceneName=\"{mission.sceneName}\"");

                float duration = (mission != null && mission.objectiveType == MissionObjectiveType.Survive && mission.targetValue > 0)
                    ? mission.targetValue
                    : LevelTimer.Instance.TotalDuration;
                LevelTimer.Instance.StartTimer(duration);
                Debug.Log($"[GameplayState] 計時器啟動，duration={duration}s（任務：{mission?.objectiveType.ToString() ?? "無"})");
            }

            // 步驟 C: 顯示本關任務簡報（若 ActiveRoom 有指定 mission 且有 briefingStep）
            // 注意：必須透過 TutorialManager 呼叫，才能讓確認輸入監聽正常運作。
            // 直接呼叫 UIManager.ShowTutorialDialogue 會讓對話框出現但無法關閉（isDialogueOpen 不會被設為 true）。
            TutorialStepData briefing = GameDB.Instance?.Campaign.ActiveRoom.mission?.briefingStep;
            if (briefing != null)
            {
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.ShowDialogue(briefing);
                }
                else
                {
                    // TutorialManager 不在場景中（例如一般戰鬥場景只有 briefing 用途）
                    // fallback：直接顯示對話框，並在 UIManager 側接手輸入
                    UIManager.Instance?.ShowTutorialDialogue(briefing);
                    Debug.LogWarning("[GameplayState] 場景中無 TutorialManager，briefing 對話框將無法透過輸入關閉。請在場景中放置 TutorialManager。");
                }
            }
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
        // 注意：HideGameplayHUD 已移到 LoadBattleSceneRoutine 載入新場景之前執行，
        // 避免在 StageClearState 等待玩家走門期間把 HUD（含 ExitPrompt）提前隱藏。
        BattleEventManager.OnRoomCleared -= HandleRoomCleared;
        BattleEventManager.OnPlayerDied -= HandlePlayerDied;
    }
    
    public void Update() 
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 按下 Y 鍵：模擬玩家走門（選擇選項 0 + 過關），完整走 StageClearState 流程
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log($"[GameplayState] Y 鍵：模擬走門（選項 0）");
            // 若 PendingOptions 有資料就 SelectOption，沒有就跳過（出口路徑不需要）
            if (GameDB.Instance?.Campaign.PendingOptions.Length > 0)
                GameDB.Instance.Campaign.SelectOption(0);
            BattleEventManager.TriggerRoomCleared();
        }

        // 按下 T 鍵：測試用 — 強制觸發獎勵流程，不論輸贏
        // 模擬敵人全滅 + 獎勵已領取，讓 DoorController 解鎖；再觸發 AllEnemiesDefeated 讓 RewardItemSpawner 生成卡牌
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[GameplayState] 偵測到按下 T 鍵，強制觸發獎勵流程（測試用）");
            // 1. 先通知敵人全滅（讓 RewardItemSpawner 生成獎勵物件）
            BattleEventManager.TriggerAllEnemiesDefeated();
        }

        // 按下 N 鍵：直接跳過結算 UI，進入下一關 roomNumber + 1 (如果下一關是 Boss 則進入 Boss 戰)
        if (Input.GetKeyDown(KeyCode.N))
        {
            int nextRoom = roomNumber + 1;
            bool nextIsBoss = GameDB.Instance?.Campaign.IsNextRoomBoss() ?? false;
            Debug.Log($"[GameplayState] 偵測到按下 N 鍵，直接跳過結算 UI！關卡切換: {roomNumber} -> {nextRoom}（Boss: {nextIsBoss}）");
            if (nextIsBoss)
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
