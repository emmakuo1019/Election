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
            Debug.Log("玩家已進入區域！");
            tips.SetActive(true);
            isPlayerNearSwitch = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家離開");
            tips.SetActive(false);
            isPlayerNearSwitch = false;
        }
    }
    
    private void OpenUpgradePanel()
    {
        SceneManager.LoadScene("TestMVP");
    }
    
    //調整自適應性UI
    
    /*private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            
            if (iconKeyboard != null && iconGamepad != null)
            {
                switch (GameDB.currentInput)
                {
                    case InputHandleHelper.InputMethod.Keyboard:
                        iconKeyboard.SetActive(true);
                        iconGamepad.SetActive(false);
                        break;
                    case InputHandleHelper.InputMethod.Gamepad:
                        iconKeyboard.SetActive(false);
                        iconGamepad.SetActive(true);
                        break;
                }
            }

            interactionPrompt.SetActive(true);
            Debug.Log("顯示互動提示");
        }
    }*/
    
}

