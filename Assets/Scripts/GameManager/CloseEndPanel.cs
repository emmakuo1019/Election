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

    public void ClosePanel()
    {
        Debug.Log("[CloseEndPanel] 按下了關閉面板按鈕！準備廣播事件...");
        
        // 嚴格遵守 UI 脫鉤原則：不直接呼叫 ChangeState，也不直接 LoadScene。
        // 只負責無腦廣播「玩家確認返回」的事件。
        BattleEventManager.TriggerReturnToHQConfirmed();
    }
}
