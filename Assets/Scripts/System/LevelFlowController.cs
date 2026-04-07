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

        if (BlockProgressManager.IsLastRoomInBlock())
        {
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

            Debug.Log("前往下一關：" + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}