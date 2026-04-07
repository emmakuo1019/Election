using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeButton : MonoBehaviour
{
    [Header("這個節點要前往的第一關場景")]
    [SerializeField] private string targetSceneName;

    public void EnterThisBlock()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("MapNodeButton：targetSceneName 沒有設定");
            return;
        }

        Time.timeScale = 1f;
        Debug.Log("玩家選擇地圖節點，前往：" + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }
}
