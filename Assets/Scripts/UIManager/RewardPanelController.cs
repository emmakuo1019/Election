using UnityEngine;

public class RewardPanelController : MonoBehaviour
{
    [Header("獎勵面板")]
    [SerializeField] private GameObject rewardPanel;

    private bool isShowing = false;
    private RewardPanelUI rewardPanelUI;

    private void Start()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
            rewardPanelUI = rewardPanel.GetComponent<RewardPanelUI>();
        }
    }

    public void ShowRewardPanel()
    {
        ShowRewardPanel(0f, 0, 0, 0, false);
    }

    public void ShowRewardPanel(float supportRate, int supporterCount, int totalVoters, int rewardMP, bool canClaimReward)
    {
        if (isShowing) return;
        isShowing = true;
        Time.timeScale = 0f;

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanelUI ??= rewardPanel.GetComponent<RewardPanelUI>();
            rewardPanelUI?.ConfigureSettlement(supportRate, supporterCount, totalVoters, rewardMP, canClaimReward);
        }
    }

    public void HideRewardPanel()
    {
        if (!isShowing) return;
        isShowing = false;
        Time.timeScale = 1f;

        //Debug.Log("關閉 Reward Panel");

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }
}
