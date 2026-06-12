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
            GameFlowManager.Instance.ChangeState(new GameplayState(1));

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
        string firstRoomScene = BlockProgressManager.StartNextCampaignBlock();
        SceneManager.LoadScene(firstRoomScene);
    }
    
}
