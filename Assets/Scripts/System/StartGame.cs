using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public GameObject tips;
    private bool isPlayerNearSwitch;
    public KeyCode interactKey = KeyCode.E;
    
    void Start()
    {

        tips.SetActive(false);
    }
    
    void Update()
    {
        if (isPlayerNearSwitch
            && tips.activeSelf
            && Input.GetKeyDown(interactKey))
        {
            OpenUpgradePanel();
        }
    }
    // TRIGGER EVENT 碰撞事件------------------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tips.SetActive(true);
            isPlayerNearSwitch = true;
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
