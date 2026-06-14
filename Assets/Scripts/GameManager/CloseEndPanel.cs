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
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ChangeState(new HQState());
        }
        else
        {
            Debug.LogWarning("GameFlowManager.Instance is null. Falling back to direct load.");
            SceneManager.LoadScene("headquarters");
        }
    }
}
