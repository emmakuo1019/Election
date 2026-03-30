using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MVPManager : MonoBehaviour
{
    public Button exitButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        exitButton.onClick.AddListener(OnExitClick);
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StartTimer();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnExitClick()
    {
        SceneManager.LoadScene("Scenes/headquarters");
    }
}
