#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 教學場景選路診斷工具。
/// 放在教學場景中，按數字鍵顯示各種診斷資訊。
/// 僅在 Editor 模式下編譯，正式版 Build 時自動排除。
/// </summary>
public class RouteSelectionDebugger : MonoBehaviour
{
    private void Update()
    {
        // 按 1：檢查 Campaign 狀態
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DiagnoseCampaignState();
        }

        // 按 2：檢查場景中的門
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            DiagnoseDoorsInScene();
        }

        // 按 3：檢查事件訂閱
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DiagnoseEventSubscriptions();
        }

        // 按 4：檢查 RoomExitController
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            DiagnoseRoomExitController();
        }

        // 按 5：檢查當前戰鬥階段
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            DiagnoseEncounterPhase();
        }

        // 按 8：手動呼叫 ShowExitDoors（測試用）
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.Log("=== [診斷] 手動呼叫 ShowExitDoors ===");
            var exitController = FindFirstObjectByType<RoomExitController>();
            if (exitController != null)
            {
                exitController.ShowExitDoors(needsRouteChoice: true);
                Debug.Log("[診斷] ShowExitDoors(true) 已執行");
            }
            else
            {
                Debug.LogError("[診斷] 找不到 RoomExitController！");
            }
        }

        // 按 9：手動觸發教學完成（測試用）
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("=== [診斷] 手動觸發教學完成 ===");
            BattleEventManager.TriggerRoomCleared();
        }
    }

    private void DiagnoseCampaignState()
    {
        Debug.Log("=== [診斷] Campaign 狀態 ===");
        
        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            Debug.LogError("Campaign 為 null！");
            return;
        }

        Debug.Log($"CurrentNodeNumber: {campaign.CurrentNodeNumber}");
        Debug.Log($"CurrentRole: {campaign.CurrentRole}");
        Debug.Log($"IsTutorialActive: {campaign.IsTutorialActive}");
        Debug.Log($"PendingOptions 數量: {campaign.PendingOptions?.Length ?? 0}");

        if (campaign.PendingOptions != null && campaign.PendingOptions.Length > 0)
        {
            for (int i = 0; i < campaign.PendingOptions.Length; i++)
            {
                var option = campaign.PendingOptions[i];
                Debug.Log($"  選項 {i}: {option.mission?.name ?? "null"} (場景: {option.sceneName})");
            }
        }

        Debug.Log($"ActiveRoom: {campaign.ActiveRoom.mission?.name ?? "null"}");
        Debug.Log("========================");
    }

    private void DiagnoseDoorsInScene()
    {
        Debug.Log("=== [診斷] 場景中的門 ===");
        
        var doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        Debug.Log($"找到 {doors.Length} 個 DoorController");

        foreach (var door in doors)
        {
            Debug.Log($"門: {door.gameObject.name}");
            Debug.Log($"  - Active: {door.gameObject.activeInHierarchy}");
            Debug.Log($"  - Position: {door.transform.position}");
            
            // 使用反射檢查私有欄位
            var type = door.GetType();
            var isUnlockedField = type.GetField("_isUnlocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isRouteDoorField = type.GetField("_isRouteDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var optionIndexField = type.GetField("optionIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (isUnlockedField != null)
                Debug.Log($"  - IsUnlocked: {isUnlockedField.GetValue(door)}");
            if (isRouteDoorField != null)
                Debug.Log($"  - IsRouteDoor: {isRouteDoorField.GetValue(door)}");
            if (optionIndexField != null)
                Debug.Log($"  - OptionIndex: {optionIndexField.GetValue(door)}");

            // 檢查 Collider
            var collider = door.GetComponent<Collider>();
            if (collider != null)
            {
                Debug.Log($"  - Collider enabled: {collider.enabled}");
                Debug.Log($"  - Collider isTrigger: {collider.isTrigger}");
            }
            else
            {
                Debug.LogWarning($"  - 沒有 Collider！");
            }
        }
        Debug.Log("========================");
    }

    private void DiagnoseEventSubscriptions()
    {
        Debug.Log("=== [診斷] 事件訂閱狀態 ===");
        
        // 檢查 BattleEventManager 的事件訂閱數量
        var type = typeof(BattleEventManager);
        
        var onRouteSelectedField = type.GetField("OnRouteSelected", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var onRoomClearedField = type.GetField("OnRoomCleared", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        if (onRouteSelectedField != null)
        {
            var evt = (System.Delegate)onRouteSelectedField.GetValue(null);
            Debug.Log($"OnRouteSelected 訂閱數: {evt?.GetInvocationList().Length ?? 0}");
            if (evt != null)
            {
                foreach (var handler in evt.GetInvocationList())
                {
                    Debug.Log($"  - {handler.Method.DeclaringType?.Name}.{handler.Method.Name}");
                }
            }
        }

        if (onRoomClearedField != null)
        {
            var evt = (System.Delegate)onRoomClearedField.GetValue(null);
            Debug.Log($"OnRoomCleared 訂閱數: {evt?.GetInvocationList().Length ?? 0}");
            if (evt != null)
            {
                foreach (var handler in evt.GetInvocationList())
                {
                    Debug.Log($"  - {handler.Method.DeclaringType?.Name}.{handler.Method.Name}");
                }
            }
        }
        
        Debug.Log("========================");
    }

    private void DiagnoseRoomExitController()
    {
        Debug.Log("=== [診斷] RoomExitController ===");
        
        var exitController = FindFirstObjectByType<RoomExitController>();
        if (exitController == null)
        {
            Debug.LogError("場景中找不到 RoomExitController！");
            Debug.LogError("這是無法選路的主要原因之一。");
            return;
        }

        Debug.Log($"RoomExitController: {exitController.gameObject.name}");
        Debug.Log($"  - Active: {exitController.gameObject.activeInHierarchy}");

        // 使用反射檢查私有欄位
        var type = exitController.GetType();
        var singleDoorField = type.GetField("singleDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var routeDoorLeftField = type.GetField("routeDoorLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var routeDoorRightField = type.GetField("routeDoorRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (singleDoorField != null)
        {
            var singleDoor = singleDoorField.GetValue(exitController) as DoorController;
            Debug.Log($"  - singleDoor: {(singleDoor != null ? singleDoor.gameObject.name : "null")}");
        }

        if (routeDoorLeftField != null)
        {
            var leftDoor = routeDoorLeftField.GetValue(exitController) as DoorController;
            Debug.Log($"  - routeDoorLeft: {(leftDoor != null ? leftDoor.gameObject.name : "null")}");
            if (leftDoor != null)
                Debug.Log($"    Active: {leftDoor.gameObject.activeInHierarchy}");
        }

        if (routeDoorRightField != null)
        {
            var rightDoor = routeDoorRightField.GetValue(exitController) as DoorController;
            Debug.Log($"  - routeDoorRight: {(rightDoor != null ? rightDoor.gameObject.name : "null")}");
            if (rightDoor != null)
                Debug.Log($"    Active: {rightDoor.gameObject.activeInHierarchy}");
        }

        Debug.Log("========================");
    }

    private void DiagnoseEncounterPhase()
    {
        Debug.Log("=== [診斷] 當前戰鬥階段 ===");
        Debug.Log($"CurrentEncounterPhase: {BattleEventManager.CurrentEncounterPhase}");
        Debug.Log("========================");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 350));
        GUILayout.Label("=== 選路診斷工具 ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);
        GUILayout.Label("按 1：檢查 Campaign 狀態");
        GUILayout.Label("按 2：檢查場景中的門");
        GUILayout.Label("按 3：檢查事件訂閱");
        GUILayout.Label("按 4：檢查 RoomExitController");
        GUILayout.Label("按 5：檢查當前戰鬥階段");
        GUILayout.Space(10);
        GUILayout.Label("按 8：手動呼叫 ShowExitDoors（強制顯示門）", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label("按 9：手動觸發教學完成（測試用）");
        GUILayout.EndArea();
    }
}

#endif
