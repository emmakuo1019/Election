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
            // 點擊時呼叫：
            if (GameDB.Instance?.Campaign.StartFormalCampaign() == true)
                GameFlowManager.Instance.ChangeState(new GameplayState(GameDB.Instance.Campaign.CurrentNodeNumber));

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
        if (GameDB.Instance?.Campaign.StartFormalCampaign() == true)
            GameFlowManager.Instance.ChangeState(new GameplayState(GameDB.Instance.Campaign.CurrentNodeNumber));
    }
    
}
