using UnityEngine;

public class RoomClearFlowController : MonoBehaviour
{
    [Header("房間選情結算器")]
    [SerializeField] private RoomResultCalculator roomResultCalculator;

    [Header("房間出口控制器")]
    [SerializeField] private RoomExitController roomExitController;

    [Header("獎勵面板控制器")]
    [SerializeField] private RewardPanelController rewardPanelController;

    private bool isResolvingRoomClear = false;

    public void OnRoomCleared(bool canClaimReward)
    {
        if (isResolvingRoomClear)
        {
            return;
        }

        isResolvingRoomClear = true;
        PauseGameplayForSettlement();
        ForceAllVotersExit();

        ShowRewardPanel(canClaimReward);
    }

    private void PauseGameplayForSettlement()
    {
        LevelTimer.Instance?.PauseTimer();
    }

    public void OnContinuePressed()
    {
        OpenExitAndFinish();
    }

    private void ShowRewardPanel(bool canClaimReward)
    {
        if (rewardPanelController == null)
        {
            rewardPanelController = FindFirstObjectByType<RewardPanelController>(FindObjectsInactive.Include);
        }

        if (rewardPanelController == null)
        {
            Debug.LogWarning("⚠️ RoomClearFlowController：找不到 RewardPanelController，直接開啟出口");
            OpenExitAndFinish();
            return;
        }

        float supportRate = roomResultCalculator != null ? roomResultCalculator.GetGlobalSupportRate() : 0f;
        int totalVoters = roomResultCalculator != null ? roomResultCalculator.GetTotalVoters() : 0;
        int playerSupporters = roomResultCalculator != null ? roomResultCalculator.GetPlayerSupporters() : 0;
        int rewardMP = roomResultCalculator != null ? roomResultCalculator.CalculateAndRewardMP() : 0;

        rewardPanelController.ShowRewardPanel(
            supportRate,
            playerSupporters,
            totalVoters,
            rewardMP,
            canClaimReward
        );
    }

    private void OpenExitAndFinish()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        playerController?.EnableMovementOnly();

        if (roomExitController == null)
        {
            roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        }

        if (roomExitController == null)
        {
            Debug.LogWarning("⚠️ RoomClearFlowController：找不到 RoomExitController，無法開啟出口");
            return;
        }

        roomExitController.UnlockExit();
        isResolvingRoomClear = false;
    }

    private void ForceAllVotersExit()
    {
        if (roomExitController == null)
        {
            roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        }

        if (roomExitController == null)
        {
            return;
        }

        Vector3 exitPosition = roomExitController.GetVoterExitPosition();
        VoterLogic[] voters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);
        foreach (VoterLogic voter in voters)
        {
            voter.BeginExitMovement(exitPosition);
        }
    }
}
