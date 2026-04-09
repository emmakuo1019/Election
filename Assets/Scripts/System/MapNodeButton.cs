using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapNodeButton : MonoBehaviour
{
    [Header("節點資料")]
    [SerializeField] private MapNodeData nodeData;

    [Header("按鈕（可選）")]
    [SerializeField] private Button button;

    [Header("節點視覺（可選）")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject availableVisual;
    [SerializeField] private GameObject completedVisual;

    private void Start()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        if (nodeData == null)
        {
            Debug.LogWarning($"{name}：MapNodeButton 沒有指定 nodeData");
            return;
        }

        if (button != null)
        {
            button.interactable = true;
        }

        if (lockedVisual != null) lockedVisual.SetActive(false);
        if (availableVisual != null) availableVisual.SetActive(true);
        if (completedVisual != null) completedVisual.SetActive(false);
    }

    public void OnClickNode()
    {
        if (nodeData == null)
        {
            Debug.LogWarning("MapNodeButton：nodeData 為空");
            return;
        }

        if (string.IsNullOrWhiteSpace(nodeData.targetSceneName))
        {
            Debug.LogWarning($"MapNodeButton：{nodeData.nodeID} 沒有設定 targetSceneName");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nodeData.targetSceneName))
        {
            Debug.LogWarning($"MapNodeButton：無法載入場景 {nodeData.targetSceneName}");
            return;
        }

        Debug.Log($"🗺️ 進入節點：{nodeData.nodeID} → 場景：{nodeData.targetSceneName}");
        SceneManager.LoadScene(nodeData.targetSceneName);
    }
}
