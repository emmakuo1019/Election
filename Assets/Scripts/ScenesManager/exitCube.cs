using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class exitCube : MonoBehaviour
{
    [Header("要返回的大地圖場景名稱")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("是否只觸發一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered) return;

        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        Debug.Log("玩家進入 ExitCube，準備返回大地圖");

        // 這裡才是真正完成一個區塊
        CampaignProgressManager.AddCompletedBlock();

        // 清掉這個區塊的房間進度
        BlockProgressManager.ClearBlockProgress();

        // 保險：恢復時間
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("ExitToMapTrigger：mapSceneName 沒有設定");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mapSceneName))
        {
            Debug.LogWarning("ExitToMapTrigger：無法載入場景 " + mapSceneName);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }
}


