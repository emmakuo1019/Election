using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S1Manager : MonoBehaviour
{
    public Button skipBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skipBtn.onClick.AddListener(OnSkipButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnSkipButtonClick()
    {
        SceneManager.LoadScene("headquarters");
    }
}
