using Unity.VisualScripting;
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
        SceneManager.LoadScene("S1");
    }

    private void OnExitBtnClick()
    {
        Application.Quit();
    }
}
