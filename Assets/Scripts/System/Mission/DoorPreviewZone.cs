using UnityEngine;

/// <summary>
/// 門口提示感應區（較大範圍）。
/// 玩家進入 → 顯示任務 tip；玩家離開 → 隱藏 tip。
/// 掛在門的子物件上，搭配較大的 Trigger Collider。
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorPreviewZone : MonoBehaviour
{
    [Tooltip("對應的 DoorController，用來讀取任務資料")]
    [SerializeField] private DoorController door;

    private void Awake()
    {
        // 確保 Collider 設為 Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[DoorPreviewZone] {gameObject.name} 的 Collider 未設為 Trigger，自動修正。");
            col.isTrigger = true;
        }

        // 如果沒有手動設定 door，嘗試自動尋找父物件或同層的 DoorController
        if (door == null)
        {
            door = GetComponentInParent<DoorController>();
            if (door == null)
            {
                door = GetComponent<DoorController>();
            }
            
            if (door != null)
            {
                Debug.Log($"[DoorPreviewZone] 自動找到 DoorController：{door.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[DoorPreviewZone] {gameObject.name} 找不到 DoorController！請在 Inspector 中設定。");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"[DoorPreviewZone] 玩家進入感應區：{gameObject.name}");
        door?.ShowTip();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"[DoorPreviewZone] 玩家離開感應區：{gameObject.name}");
        UIManager.Instance?.HideTutorialTips();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"[DoorPreviewZone] 玩家進入感應區（2D）：{gameObject.name}");
        door?.ShowTip();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"[DoorPreviewZone] 玩家離開感應區（2D）：{gameObject.name}");
        UIManager.Instance?.HideTutorialTips();
    }
}
