using UnityEngine;

/// <summary>
/// [已廢棄 Obsolete]
/// 原有的過關抽卡 UI 控制器已不再使用，此腳本已清空邏輯。
/// 所有的結算流程已移交給 StageClearState 配合 UIManager.StartStageClearSequence 處理。
/// 請從場景中移除掛載此腳本的 GameObject 以保持整潔。
/// </summary>
[System.Obsolete("已被 StageClearState 與 UIManager 取代，請移除此腳本的掛載", false)]
public class RewardPanelController : MonoBehaviour
{
    public void ShowRewardPanel()
    {
        Debug.LogWarning("RewardPanelController 已廢棄，請不要再呼叫它。");
    }

    public void ShowRewardPanel(float supportRate, int supporterCount, int totalVoters, int rewardMP, bool canClaimReward)
    {
    }

    public void HideRewardPanel()
    {
    }
}
