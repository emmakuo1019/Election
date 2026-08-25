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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        door?.ShowTip();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        UIManager.Instance?.HideTutorialTips();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        door?.ShowTip();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        UIManager.Instance?.HideTutorialTips();
    }
}
