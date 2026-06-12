using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CloseEndPanel : MonoBehaviour
{
    public Button closeButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        closeButton.onClick.AddListener(ClosePanel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ClosePanel()
    {
        SceneManager.LoadScene("headquarters");
    }
}
