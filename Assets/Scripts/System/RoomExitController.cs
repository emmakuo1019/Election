using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomExitController : MonoBehaviour
{
    [Header("出口外觀（可選）")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    [Header("出口目標場景")]
    [SerializeField] private string targetSceneName;

    [Header("是否為區塊最後一房出口")]
    [SerializeField] private bool isFinalRoomExit = false;

    [Header("這個區塊對應的大地圖節點（第三房出口才需要）")]
    [SerializeField] private MapNodeData currentMapNode;

    [Header("是否只觸發一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool isUnlocked = false;
    private bool hasTriggered = false;

    private void Start()
    {
        UpdateVisual();
    }

    public void UnlockExit()
    {
        isUnlocked = true;
        UpdateVisual();
        Debug.Log("🚪 出口已開啟");
    }

    public void LockExit()
    {
        isUnlocked = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked) return;
        if (triggerOnlyOnce && hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        Debug.Log($"🚪 玩家進入出口，準備前往：{targetSceneName}");

        Time.timeScale = 1f;

        if (isFinalRoomExit)
        {
            CampaignProgressManager.AddCompletedBlock();
            BlockProgressManager.ClearBlockProgress();

            Debug.Log("✅ 已完成一個區塊，返回大地圖");
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("RoomExitController：targetSceneName 沒有設定");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning("RoomExitController：無法載入場景：" + targetSceneName);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
