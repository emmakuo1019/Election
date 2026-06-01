using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapNodeButton : MonoBehaviour
{
    private const string BossSceneName = "TestSmallBoss";

    public enum RouteType
    {
        StandardBlock,
        BossStage
    }

    [Header("路線類型")]
    [SerializeField] private RouteType routeType = RouteType.StandardBlock;

    [Header("線性區塊順序")]
    [SerializeField] private int blockOrder = 1;

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
        bool isCompleted = IsCompleted();
        bool isAvailable = IsAvailable();

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
        if (!IsAvailable())
        {
            return;
        }

        string targetSceneName = GetTargetSceneName();
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"MapNodeButton：無法載入場景 {targetSceneName}");
            return;
        }
        SceneManager.LoadScene(targetSceneName);
    }

    private bool IsCompleted()
    {
        if (routeType == RouteType.BossStage)
        {
            return false;
        }

        int normalizedBlockOrder = Mathf.Clamp(blockOrder, 1, CampaignProgressManager.GetTotalBlockCount());
        return CampaignProgressManager.IsBlockCompleted(normalizedBlockOrder);
    }

    private bool IsAvailable()
    {
        if (routeType == RouteType.BossStage)
        {
            return CampaignProgressManager.CanEnterBossStage();
        }

        int normalizedBlockOrder = Mathf.Clamp(blockOrder, 1, CampaignProgressManager.GetTotalBlockCount());
        return CampaignProgressManager.CanEnterBlock(normalizedBlockOrder);
    }

    private string GetTargetSceneName()
    {
        if (routeType == RouteType.BossStage)
        {
            return BossSceneName;
        }

        int normalizedBlockOrder = Mathf.Clamp(blockOrder, 1, CampaignProgressManager.GetTotalBlockCount());
        return BlockProgressManager.StartRandomBlock(normalizedBlockOrder);
    }
}
