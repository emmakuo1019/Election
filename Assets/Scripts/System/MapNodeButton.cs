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

    [Header("節點縮放")]
    [SerializeField] private float availableScaleMultiplier = 1.08f;

    private Vector3 defaultScale;

    private void Start()
    {
        defaultScale = transform.localScale;
        RefreshState();
    }

    public void RefreshState()
    {
        if (nodeData == null)
        {
            Debug.LogWarning($"{name}：MapNodeButton 沒有指定 nodeData");
            return;
        }

        if (MapProgressManager.Instance == null)
        {
            Debug.LogWarning("MapProgressManager 尚未存在");
            return;
        }

        bool isCompleted = MapProgressManager.Instance.IsNodeCompleted(nodeData.nodeID);
        bool isAvailable = MapProgressManager.Instance.CanEnterNode(nodeData.nodeID);

        if (button != null)
        {
            button.interactable = isAvailable;
        }

        if (lockedVisual != null) lockedVisual.SetActive(!isAvailable && !isCompleted);
        if (availableVisual != null) availableVisual.SetActive(isAvailable);
        if (completedVisual != null) completedVisual.SetActive(isCompleted);

        transform.localScale = isAvailable ? defaultScale * availableScaleMultiplier : defaultScale;
    }

    public void OnClickNode()
    {
        if (nodeData == null)
        {
            Debug.LogWarning("MapNodeButton：nodeData 為空");
            return;
        }

        if (MapProgressManager.Instance == null)
        {
            Debug.LogWarning("MapProgressManager 不存在");
            return;
        }

        if (!MapProgressManager.Instance.CanEnterNode(nodeData.nodeID))
        {
            Debug.Log($"⛔ 此節點目前不可進入：{nodeData.nodeID}");
            return;
        }

        string firstRoomScene = BlockProgressManager.StartRandomBlock();
        if (!Application.CanStreamedLevelBeLoaded(firstRoomScene))
        {
            Debug.LogWarning($"MapNodeButton：無法載入場景 {firstRoomScene}");
            return;
        }

        Debug.Log($"🗺️ 進入節點：{nodeData.nodeID} → 場景：{firstRoomScene}");
        SceneManager.LoadScene(firstRoomScene);
    }
}
