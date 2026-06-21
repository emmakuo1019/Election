
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S0Manager : MonoBehaviour
{
    public Button newGameButton;
    public Button exitButton;
    void Start()
    {
        newGameButton.onClick.AddListener(OnNewGameBtnClick);
        exitButton.onClick.AddListener(OnExitBtnClick);
    }
    
    private void OnNewGameBtnClick()
    {
        GameFlowManager.Instance.ChangeState(new HQState());
    }

    private void OnExitBtnClick()
    {
        Application.Quit();
    }
}
