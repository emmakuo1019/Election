using UnityEngine;
using UnityEngine.UI;

public class MapProgressUI : MonoBehaviour
{
    [Header("進度文字")]
    [SerializeField] private Text progressText;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (progressText == null)
        {
            Debug.LogWarning("MapProgressUI：progressText 沒有指定");
            return;
        }

        int completed = CampaignProgressManager.GetCompletedBlockCount();
        progressText.text = "已完成區塊：" + completed + " / 3";

        Debug.Log("更新地圖進度 UI：" + completed + " / 3");
    }
}