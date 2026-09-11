using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// S1 劇情介紹場景管理器。
/// 劇情播放完畢後自動進入總部（HQState）。
/// </summary>
public class S1Manager : MonoBehaviour
{
    // 移除：跳過按鈕已移除，劇情必須完整播放
    // public Button skipBtn;

    void Start()
    {
        // 移除：跳過按鈕已移除
        // skipBtn.onClick.AddListener(OnSkipButtonClick);
    }

    void Update()
    {
        // 未來可在此處監聽劇情播放完畢事件，自動切換到 HQState
    }

    // 移除：跳過功能已移除
    /*
    private void OnSkipButtonClick()
    {
        GameFlowManager.Instance.ChangeState(new HQState());
    }
    */
}
