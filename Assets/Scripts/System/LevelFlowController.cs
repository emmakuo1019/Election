using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFlowController : MonoBehaviour
{
    [Header("一般下一關場景名稱")]
    [SerializeField] private string nextSceneName;

    [Header("大地圖場景名稱")]
    [SerializeField] private string mapSceneName = "MapScene";

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;

        if (BlockProgressManager.HasBlockProgress() && BlockProgressManager.IsLastRoomInBlock())
        {
            if (string.IsNullOrEmpty(mapSceneName))
            {
                Debug.LogWarning("大地圖場景名稱沒有設定");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(mapSceneName))
            {
                Debug.LogWarning("大地圖場景無法載入：" + mapSceneName);
                return;
            }

            Debug.Log("本區塊最後一房，前往大地圖：" + mapSceneName);
            BlockProgressManager.ClearBlockProgress();
            SceneManager.LoadScene(mapSceneName);
        }
        else
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("下一關場景名稱沒有設定");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogWarning("下一關場景無法載入：" + nextSceneName);
                return;
            }

            Debug.Log("前往下一關：" + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
