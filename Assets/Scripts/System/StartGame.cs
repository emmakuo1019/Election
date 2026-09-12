using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public GameObject tips;
    private bool isPlayerNearSwitch;
    
    void Start()
    {

        tips.SetActive(false);
    }
    
    void Update()
    {
        if (isPlayerNearSwitch
            && tips.activeSelf)
        {
            OpenUpgradePanel();
        }
    }
    // TRIGGER EVENT 碰撞事件------------------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 檢查當前狀態，防止在非 HQ 場景中觸發
            var currentState = GameFlowManager.Instance?.CurrentState;
            
            if (currentState is TutorialState)
            {
                Debug.Log("[StartGame] 在教學場景中，忽略觸發");
                return;
            }

            if (currentState is GameplayState or StageClearState)
            {
                Debug.LogWarning($"[StartGame] 在戰鬥/結算場景中被觸發！場景配置可能有誤。" +
                               $"當前節點: {GameDB.Instance?.Campaign.CurrentNodeNumber}");
                return;
            }

            // 額外保護：檢查戰役是否已在進行中
            var campaign = GameDB.Instance?.Campaign;
            if (campaign != null && campaign.CurrentNodeNumber > 0)
            {
                Debug.LogWarning($"[StartGame] 戰役已在進行中（節點 {campaign.CurrentNodeNumber}），忽略觸發");
                return;
            }

            // 只有在 HQ 或遊戲開始時才允許開始新戰役
            if (campaign?.StartFormalCampaign() == true)
                GameFlowManager.Instance.ChangeState(new TutorialState());
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tips.SetActive(false);
            isPlayerNearSwitch = false;
        }
    }
    
    private void OpenUpgradePanel()
    {
        // 防止在非 HQ 場景中觸發
        var currentState = GameFlowManager.Instance?.CurrentState;
        if (currentState is GameplayState or StageClearState or TutorialState)
        {
            Debug.LogWarning("[StartGame] OpenUpgradePanel 在非 HQ 場景中被呼叫，忽略");
            return;
        }

        var campaign = GameDB.Instance?.Campaign;
        if (campaign != null && campaign.CurrentNodeNumber > 0)
        {
            Debug.LogWarning($"[StartGame] 戰役已在進行中（節點 {campaign.CurrentNodeNumber}），忽略");
            return;
        }

        if (campaign?.StartFormalCampaign() == true)
            GameFlowManager.Instance.ChangeState(new TutorialState());
    }
    
}
