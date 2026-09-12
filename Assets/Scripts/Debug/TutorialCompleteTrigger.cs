#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 教學完成觸發器 - 暫時用來測試選路門顯示
/// 玩家走進這個 Trigger 會強制觸發教學完成流程
/// 僅在 Editor 模式下編譯，正式版 Build 時自動排除。
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialCompleteTrigger : MonoBehaviour
{
    [Header("測試設定")]
    [Tooltip("是否在教學場景啟用此觸發器")]
    [SerializeField] private bool enableInTutorial = true;

    private bool _hasTriggered = false;

    private void Start()
    {
        // 確保 Collider 是 Trigger
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // 如果不在教學場景中，禁用此腳本
        if (!enableInTutorial || GameDB.Instance?.Campaign.IsTutorialActive != true)
        {
            enabled = false;
            Debug.Log("[TutorialCompleteTrigger] 不在教學場景中，已禁用");
        }
        else
        {
            Debug.Log("[TutorialCompleteTrigger] 已啟用，等待玩家觸發");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        
        if (!other.CompareTag("Player")) return;

        _hasTriggered = true;
        Debug.Log("=== [TutorialCompleteTrigger] 玩家觸發！強制執行教學完成流程 ===");

        // 方法 1：觸發 RoomCleared 事件
        BattleEventManager.TriggerRoomCleared();
        Debug.Log("[TutorialCompleteTrigger] 已觸發 TriggerRoomCleared()");

        // 如果方法 1 沒效，等 1 秒後直接呼叫 ShowExitDoors
        StartCoroutine(FallbackShowDoors());
    }

    private System.Collections.IEnumerator FallbackShowDoors()
    {
        yield return new WaitForSeconds(1f);

        // 檢查門是否已經顯示
        var doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        bool anyRouteDoorActive = false;
        foreach (var door in doors)
        {
            if (door.gameObject.activeInHierarchy)
            {
                anyRouteDoorActive = true;
                break;
            }
        }

        if (!anyRouteDoorActive)
        {
            Debug.LogWarning("[TutorialCompleteTrigger] 1 秒後門仍未顯示，使用 Fallback 方法");
            
            var campaign = GameDB.Instance?.Campaign;
            if (campaign != null)
            {
                // 強制啟動正式戰役
                if (campaign.StartFormalCampaign())
                {
                    Debug.Log($"[TutorialCompleteTrigger] StartFormalCampaign() 成功，PendingOptions 數量: {campaign.PendingOptions?.Length ?? 0}");
                    
                    // 強制顯示門
                    var exitController = FindFirstObjectByType<RoomExitController>();
                    if (exitController != null)
                    {
                        Debug.Log("[TutorialCompleteTrigger] 找到 RoomExitController，呼叫 ShowExitDoors(true)");
                        exitController.ShowExitDoors(needsRouteChoice: true);
                    }
                    else
                    {
                        Debug.LogError("[TutorialCompleteTrigger] 找不到 RoomExitController！");
                    }
                }
                else
                {
                    Debug.LogError("[TutorialCompleteTrigger] StartFormalCampaign() 失敗！");
                }
            }
        }
        else
        {
            Debug.Log("[TutorialCompleteTrigger] 門已正常顯示，無需 Fallback");
        }
    }

    private void OnGUI()
    {
        if (!enableInTutorial || !enabled) return;

        GUILayout.BeginArea(new Rect(10, Screen.height - 100, 400, 80));
        GUILayout.Label("=== 教學完成觸發器 ===", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Label(_hasTriggered ? "已觸發" : "等待玩家走進觸發區域");
        GUILayout.EndArea();
    }
}

#endif
