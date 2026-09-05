using UnityEngine;
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

        // 地圖 UI 只能請戰役流程控制器推進；不能自行載入場景或跳過生命週期。
        CampaignData campaign = GameDB.Instance?.Campaign;
        if (campaign?.StartFormalCampaign() == true)
            GameFlowManager.Instance?.ChangeState(new GameplayState(campaign.CurrentNodeNumber));
    }

    private bool IsCompleted()
    {
        return false; // 舊地圖 Block UI 已停用；正式流程使用 CampaignDefinition。
    }

    private bool IsAvailable()
    {
        if (GameDB.Instance == null || GameDB.Instance.Campaign == null) return false;

        return routeType == RouteType.StandardBlock && GameDB.Instance.Campaign.CurrentNodeNumber == 0;
    }

}
