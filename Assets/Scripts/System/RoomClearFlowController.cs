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
    private bool hasPendingSettlement = false;
    private bool canClaimReward;
    private float pendingSupportRate;
    private int pendingTotalVoters;
    private int pendingPlayerSupporters;
    private int pendingRewardMP;

    public void OnRoomCleared(bool canClaimReward)
    {
        if (isResolvingRoomClear)
        {
            return;
        }

        isResolvingRoomClear = true;
        this.canClaimReward = canClaimReward;

        PauseGameplayForSettlement();
        ForceAllVotersExit();
        CacheSettlementData();
        UnlockExitForSettlement();
    }

    public bool HasPendingSettlement()
    {
        return hasPendingSettlement;
    }

    public void ShowSettlementAtExit()
    {
        if (!isResolvingRoomClear || !hasPendingSettlement)
        {
            return;
        }

        ShowRewardPanel();
    }

    public void OnContinuePressed()
    {
        hasPendingSettlement = false;
        ProceedToNextScene();
    }

    private void PauseGameplayForSettlement()
    {
        LevelTimer.Instance?.PauseTimer();
    }

    private void CacheSettlementData()
    {
        pendingSupportRate = roomResultCalculator != null ? roomResultCalculator.GetGlobalSupportRate() : 0f;
        pendingTotalVoters = roomResultCalculator != null ? roomResultCalculator.GetTotalVoters() : 0;
        pendingPlayerSupporters = roomResultCalculator != null ? roomResultCalculator.GetPlayerSupporters() : 0;
        pendingRewardMP = roomResultCalculator != null ? roomResultCalculator.CalculateAndRewardMP() : 0;
    }

    private void UnlockExitForSettlement()
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

        hasPendingSettlement = true;
        roomExitController.UnlockExit();
    }

    private void ShowRewardPanel()
    {
        if (rewardPanelController == null)
        {
            rewardPanelController = FindFirstObjectByType<RewardPanelController>(FindObjectsInactive.Include);
        }

        if (rewardPanelController == null)
        {
            Debug.LogWarning("⚠️ RoomClearFlowController：找不到 RewardPanelController，直接前往下一場景");
            ProceedToNextScene();
            return;
        }

        rewardPanelController.ShowRewardPanel(
            pendingSupportRate,
            pendingPlayerSupporters,
            pendingTotalVoters,
            pendingRewardMP,
            canClaimReward
        );
    }

    private void ProceedToNextScene()
    {
        if (roomExitController == null)
        {
            roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        }

        if (roomExitController == null)
        {
            Debug.LogWarning("⚠️ RoomClearFlowController：找不到 RoomExitController，無法前往下一場景");
            return;
        }

        roomExitController.ProceedToNextScene();
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
