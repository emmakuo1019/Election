using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 教學關卡流程狀態。
/// 載入 TeachScenes 場景，啟動 HUD，並在教學完成後
/// 透過 BattleEventManager.TriggerRoomCleared() 銜接回正常流程。
/// </summary>
public class TutorialState : IState
{
    private const string TutorialSceneName = "TeachScenes";

    public void Enter()
    {
        Debug.Log("[TutorialState] Enter - 載入教學場景");

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.StartCoroutine(LoadTutorialSceneRoutine());

        BattleEventManager.OnRoomCleared += HandleTutorialComplete;
        BattleEventManager.OnPlayerDied  += HandlePlayerDied;
    }

    private IEnumerator LoadTutorialSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(TutorialSceneName, LoadSceneMode.Single);
        if (asyncLoad == null)
        {
            Debug.LogWarning($"[TutorialState] 找不到場景 {TutorialSceneName}，請確認已加入 Build Settings。");
            yield break;
        }

        while (!asyncLoad.isDone)
            yield return null;

        // 安全檢查：若狀態已切走則中止
        if (GameFlowManager.Instance?.CurrentState != this) yield break;

        Debug.Log("[TutorialState] 教學場景載入完成");

        // 顯示 HUD（HP、MP、選票 bar 等）
        UIManager.Instance?.ShowGameplayHUD();

        // 教學場景不開倒數計時，LevelTimer 留給設計師手動設定
    }

    private void HandleTutorialComplete()
    {
        Debug.Log("[TutorialState] 教學完成，前往第一關");
        // 教學結束後進入正式第一關（房號 1）
        GameFlowManager.Instance?.ChangeState(new GameplayState(1));
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[TutorialState] 玩家死亡，重新載入教學");
        // 教學死亡直接重跑教學，不顯示結算
        GameFlowManager.Instance?.ChangeState(new TutorialState());
    }

    public void Exit()
    {
        Debug.Log("[TutorialState] Exit");
        UIManager.Instance?.HideGameplayHUD();

        BattleEventManager.OnRoomCleared -= HandleTutorialComplete;
        BattleEventManager.OnPlayerDied  -= HandlePlayerDied;
    }

    public void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 按 T 跳過教學，直接進第一關
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[TutorialState] 按下 T，跳過教學");
            BattleEventManager.TriggerRoomCleared();
        }
#endif
    }

    public void PhysicsUpdate() { }
}
