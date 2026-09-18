using UnityEngine;

/// <summary>
/// 教學專用的任務事件對話觸發器。
/// 
/// 職責：
///   1. 監聽特定遊戲事件（如敵人全滅、選民轉化等）
///   2. 事件發生時自動觸發指定的教學對話
///   3. 確保同一個 trigger 不會重複觸發第二次
/// 
/// 使用方式：
///   1. 將此腳本掛在教學場景的任意 GameObject 上（建議新建一個空物件命名為 "TutorialTriggers"）
///   2. 在 Inspector 中指定要在哪個事件發生時顯示哪段對話
///   3. 此腳本只在教學場景中啟動（非教學場景會自動 disable）
/// 
/// 注意：
///   此腳本是輕量級教學專用邏輯，不依賴 MissionTracker，不影響正式關卡。
/// </summary>
public class MissionDialogueTrigger : MonoBehaviour
{
    [Header("教學事件觸發配置")]
    [Tooltip("當敵人全滅時觸發的對話（可選，留空則不觸發）")]
    [SerializeField] private TutorialStepData onAllEnemiesDefeatedDialogue;

    // ── 內部狀態 ──────────────────────────────────────────────────────

    /// <summary>記錄 AllEnemiesDefeated 事件是否已觸發過</summary>
    private bool _hasTriggeredAllEnemiesDefeated = false;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Start()
    {
        // 只在教學場景啟用，非教學場景自動禁用
        if (GameDB.Instance?.Campaign.IsTutorialActive != true)
        {
            Debug.Log("[MissionDialogueTrigger] 非教學場景，禁用此腳本");
            enabled = false;
            return;
        }

        Debug.Log("[MissionDialogueTrigger] 教學場景中啟動，開始監聽任務事件");
    }

    private void OnEnable()
    {
        // 訂閱敵人全滅事件
        BattleEventManager.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }

    private void OnDisable()
    {
        // 取消訂閱，避免記憶體洩漏
        BattleEventManager.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    // ── 事件處理 ─────────────────────────────────────────────────────

    /// <summary>
    /// 處理「敵人全滅」事件。
    /// </summary>
    private void HandleAllEnemiesDefeated()
    {
        // 防止重複觸發
        if (_hasTriggeredAllEnemiesDefeated)
        {
            Debug.Log("[MissionDialogueTrigger] AllEnemiesDefeated 已觸發過，跳過");
            return;
        }

        // 檢查是否有設定對話資料
        if (onAllEnemiesDefeatedDialogue == null)
        {
            Debug.Log("[MissionDialogueTrigger] AllEnemiesDefeated 觸發，但未設定對話資料，跳過");
            return;
        }

        // 標記為已觸發
        _hasTriggeredAllEnemiesDefeated = true;

        Debug.Log($"[MissionDialogueTrigger] AllEnemiesDefeated 觸發，顯示對話：{onAllEnemiesDefeatedDialogue.name}");

        // 顯示對話
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowDialogue(onAllEnemiesDefeatedDialogue);
        }
        else
        {
            Debug.LogWarning("[MissionDialogueTrigger] TutorialManager.Instance 為 null，無法顯示對話！");
        }
    }
}
