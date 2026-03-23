using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S0Manager : MonoBehaviour
{
    public Button newGameButton;
    public Button exitButton;
    
    private InputAction upAction;
    private InputAction downAction;
    private InputAction selectedAction;
    
    private int btnIndex = 0;
    private bool isDirty = true;
    
    // LIFECYCLE 生命週期 --------------------------------------------------------------------

    private void Awake()
    {
        //ResetGameManager();
    }

    private void OnEnable()
    {
        InputSystem.actions.FindActionMap("Player").Enable();
        InputSystem.actions.FindActionMap("UI").Enable();
    }
    
    void Start()
    {
        Time.timeScale = 1f;

        upAction = InputSystem.actions.FindAction("Up");
        downAction = InputSystem.actions.FindAction("Down");
        selectedAction = InputSystem.actions.FindAction("Selected");

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameBtnClick);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitBtnClick);
        }
    }

    // Update is called once per frame

    private void Update()
    {
        if (upAction.triggered)
        {
            btnIndex++;
            if (btnIndex > 1)
            {
                btnIndex = 0;
            }

            isDirty = true;
        }

        if (downAction.triggered)
        {
            btnIndex--;
            if (btnIndex < 0)
            {
                btnIndex = 1;
            }

            isDirty = true;
        }

        if (selectedAction.triggered)
        {
            switch (btnIndex)
            {
                case 0:
                    OnNewGameBtnClick();
                    break;
                case 1:
                    OnExitBtnClick();
                    break;
            }
        }


        if (isDirty)
        {
            //update ui
            switch (btnIndex)
            {
                case 0:
                    newGameButton.transform.localScale = Vector3.one * 1.5f;
                    exitButton.transform.localScale = Vector3.one;
                    break;
                case 1:
                    newGameButton.transform.localScale = Vector3.one;
                    exitButton.transform.localScale = Vector3.one * 1.5f;
                    break;
            }

            isDirty = false;
        }
    }

    private void OnDisable()
    {
        //InputSystem.actions.FindActionMap("Player").Disable();
        //InputSystem.actions.FindActionMap("UI").Disable();
    }

    // PRIVATE FUNCTION 內部方法---------------------------------------------------------------
    //private void ResetGameManager()
    //{
      //  if (GameManager.Instance != null)
        //{
          //  GameManager.Instance.totalItemsCollected = 0;
        //}
    //}
    // BUTTON FUNCTION  ----------------------------------------------------------------------
    private void OnNewGameBtnClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("S1");
    }

    private void OnExitBtnClick()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
