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
            var exitController = FindFirstObjectByType<RoomExitController>();
            if (exitController != null)
            {
                exitController.ShowExitDoors(needsRouteChoice: true);
            }
            else
            {
            }
        }

        // 按 9：手動觸發教學完成（測試用）
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            BattleEventManager.TriggerRoomCleared();
        }
    }

    private void DiagnoseCampaignState()
    {
        
        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            return;
        }


        if (campaign.PendingOptions != null && campaign.PendingOptions.Length > 0)
        {
            for (int i = 0; i < campaign.PendingOptions.Length; i++)
            {
                var option = campaign.PendingOptions[i];
            }
        }

    }

    private void DiagnoseDoorsInScene()
    {
        
        var doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);

        foreach (var door in doors)
        {
        }
    }

    private void DiagnoseEventSubscriptions()
    {
        
        // 檢查 BattleEventManager 的事件訂閱數量
        var type = typeof(BattleEventManager);
        
        var onRouteSelectedField = type.GetField("OnRouteSelected", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var onRoomClearedField = type.GetField("OnRoomCleared", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        if (onRouteSelectedField != null)
        {
            var evt = (System.Delegate)onRouteSelectedField.GetValue(null);
            if (evt != null)
            {
                foreach (var handler in evt.GetInvocationList())
                {
                }
            }
        }

        if (onRoomClearedField != null)
        {
            var evt = (System.Delegate)onRoomClearedField.GetValue(null);
            if (evt != null)
            {
                foreach (var handler in evt.GetInvocationList())
                {
                }
            }
        }
        
    }

    private void DiagnoseRoomExitController()
    {
        
        var exitController = FindFirstObjectByType<RoomExitController>();
        if (exitController == null)
        {
            return;
        }


        // 使用反射檢查私有欄位
        var type = exitController.GetType();
        var singleDoorField = type.GetField("singleDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var routeDoorLeftField = type.GetField("routeDoorLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var routeDoorRightField = type.GetField("routeDoorRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);



    }

    private void DiagnoseEncounterPhase()
    {
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
