using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeButton : MonoBehaviour
{
    [Header("這個節點要前往的第一關場景")]
    [SerializeField] private string targetSceneName;

    private bool isLoadingScene = false;

    public void EnterThisBlock()
    {
        if (isLoadingScene)
        {
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("MapNodeButton：targetSceneName 沒有設定");
            return;
        }

        Time.timeScale = 1f;
        Debug.Log("玩家選擇地圖節點，前往：" + targetSceneName);
        StartCoroutine(LoadSceneAsyncRoutine(targetSceneName));
    }

    private System.Collections.IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        isLoadingScene = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            isLoadingScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        isLoadingScene = false;
    }
}
