using UnityEngine;

public class RewardPanelController : MonoBehaviour
{
    [Header("獎勵面板")]
    [SerializeField] private GameObject rewardPanel;

    private bool isShowing = false;

    private void Start()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    public void ShowRewardPanel()
    {
        if (isShowing) return;
        isShowing = true;

        Debug.Log("顯示 Reward Panel");

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void HideRewardPanel()
    {
        if (!isShowing) return;
        isShowing = false;

        Debug.Log("關閉 Reward Panel");

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }
}