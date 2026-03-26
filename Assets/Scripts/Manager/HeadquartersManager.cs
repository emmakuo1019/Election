using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeadquartersManager : MonoBehaviour
{
    public Button backBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        backBtn.onClick.AddListener(OnBackButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnBackButtonClick()
    {
        SceneManager.LoadScene("S0");
    }
}
