using UnityEngine;

/// <summary>
/// 管理場景中的出口門和選民退場。
/// 
/// 職責：
/// 1. 根據關卡類型啟用對應的門（單門或雙門）
/// 2. 將下一關任務資料分發給門（讓門顯示對應外觀）
/// 3. 提供選民退場目標位置
/// 4. 處理 Survive 任務的出口解鎖
/// </summary>
public class RoomExitController : MonoBehaviour
{
    private const float DefaultVoterExitOffset = 6f;

    [Header("門引用")]
    [Tooltip("普通關卡使用的單一出口門")]
    [SerializeField] private DoorController singleDoor;
    
    [Tooltip("選路關卡使用的左門（對應 PendingOptions[0]）")]
    [SerializeField] private DoorController routeDoorLeft;
    
    [Tooltip("選路關卡使用的右門（對應 PendingOptions[1]）")]
    [SerializeField] private DoorController routeDoorRight;

    [Header("選民退場設定")]
    [Tooltip("任務完成後，選民退場的目標位置")]
    [SerializeField] private Transform voterExitTarget;
    [SerializeField] private float voterExitForwardOffset = DefaultVoterExitOffset;

    [Header("舊場景相容設定")]
    [Tooltip("是否在 Start() 時自動解鎖（教學關卡或舊場景用）")]
    [SerializeField] private bool unlockOnStart = false;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        // 移除 OnRewardCollected 訂閱，避免與 StageClearState 重複處理
        // BattleEventManager.OnRewardCollected += HandleRewardCollected;
        BattleEventManager.OnSurvivalTimeUp += HandleSurvivalTimeUp;
    }

    private void OnDisable()
    {
        // BattleEventManager.OnRewardCollected -= HandleRewardCollected;
        BattleEventManager.OnSurvivalTimeUp -= HandleSurvivalTimeUp;
    }

    private void Start()
    {
        // 初始狀態：所有門都隱藏
        HideAllDoors();

        // 舊場景相容：自動解鎖
        if (unlockOnStart && singleDoor != null)
        {
            singleDoor.gameObject.SetActive(true);
            singleDoor.ForceUnlock();
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 取得選民退場的目標位置。
    /// </summary>
    public Vector3 GetVoterExitPosition()
    {
        if (voterExitTarget != null)
        {
            return voterExitTarget.position;
        }

        float offset = Mathf.Max(voterExitForwardOffset, DefaultVoterExitOffset);
        return transform.position + transform.forward * offset;
    }

    /// <summary>
    /// 啟用對應的門（由 StageClearState 調用）。
    /// needsRouteChoice = true → 啟用雙門（選路）
    /// needsRouteChoice = false → 啟用單門（下一關）
    /// </summary>
    public void ShowExitDoors(bool needsRouteChoice)
    {
        HideAllDoors();

        if (needsRouteChoice)
        {
            ShowRouteDoors();
        }
        else
        {
            ShowSingleDoor();
        }
    }

    /// <summary>
    /// 觸發選民退場動畫（未來擴充）。
    /// </summary>
    public void TriggerVoterRetreat()
    {
        // TODO: 未來實作
        // 1. 找到場上所有 VoterLogic
        // 2. 讓他們走向 GetVoterExitPosition()
        // 3. 到達後 Destroy
        Debug.Log("[RoomExitController] TriggerVoterRetreat（未實作）");
    }

    // ── 事件處理 ──────────────────────────────────────────────────────

    /// <summary>
    /// Survive 任務時間到，解鎖出口。
    /// </summary>
    private void HandleSurvivalTimeUp()
    {
        if (singleDoor != null)
        {
            singleDoor.gameObject.SetActive(true);
            singleDoor.ForceUnlock();
            Debug.Log("[RoomExitController] Survive 時間到，解鎖出口。");
        }
    }

    // ── 內部邏輯 ──────────────────────────────────────────────────────

    private void ShowSingleDoor()
    {
        if (singleDoor == null)
        {
            Debug.LogError("[RoomExitController] singleDoor 未設定！");
            return;
        }

        singleDoor.gameObject.SetActive(true);
        
        // 直接解鎖單門（不需要等待敵人全滅/獎勵收集，因為已經走完流程才會到這裡）
        singleDoor.ForceUnlock();
        
        Debug.Log("[RoomExitController] 啟用單門模式並解鎖");
    }

    private void ShowRouteDoors()
    {
        Debug.Log("[RoomExitController] ShowRouteDoors 被調用");
        
        if (routeDoorLeft == null || routeDoorRight == null)
        {
            Debug.LogError($"[RoomExitController] 選路門未設定！Left={routeDoorLeft}, Right={routeDoorRight}");
            return;
        }
        Debug.Log($"[RoomExitController] ✓ 雙門引用存在 - Left: {routeDoorLeft.gameObject.name}, Right: {routeDoorRight.gameObject.name}");

        var options = GameDB.Instance?.Campaign.PendingOptions;
        if (options == null || options.Length != 2)
        {
            Debug.LogError($"[RoomExitController] PendingOptions 數量不正確！長度={options?.Length ?? 0}");
            return;
        }
        Debug.Log("[RoomExitController] ✓ PendingOptions 有 2 個選項");

        // 啟用雙門並配置選路索引
        Debug.Log("[RoomExitController] 設定左門為 Active...");
        routeDoorLeft.gameObject.SetActive(true);
        Debug.Log($"[RoomExitController] 左門 Active 狀態: {routeDoorLeft.gameObject.activeInHierarchy}");
        
        Debug.Log("[RoomExitController] 設定右門為 Active...");
        routeDoorRight.gameObject.SetActive(true);
        Debug.Log($"[RoomExitController] 右門 Active 狀態: {routeDoorRight.gameObject.activeInHierarchy}");

        Debug.Log("[RoomExitController] 配置左門路線索引 0...");
        routeDoorLeft.ConfigureRoute(0);   // 左門對應 PendingOptions[0]
        
        Debug.Log("[RoomExitController] 配置右門路線索引 1...");
        routeDoorRight.ConfigureRoute(1);  // 右門對應 PendingOptions[1]

        Debug.Log($"[RoomExitController] ✓ 雙門配置完成 - 左：{options[0].mission.objectiveType}，右：{options[1].mission.objectiveType}");
    }

    private void HideAllDoors()
    {
        if (singleDoor != null) singleDoor.gameObject.SetActive(false);
        if (routeDoorLeft != null) routeDoorLeft.gameObject.SetActive(false);
        if (routeDoorRight != null) routeDoorRight.gameObject.SetActive(false);
    }
}
