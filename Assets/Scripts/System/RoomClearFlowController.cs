using UnityEngine;
using System.Collections;

public class RoomClearFlowController : MonoBehaviour
{
    [Header("這個房間結束後是否顯示選情快報")]
    [SerializeField] private bool needMiniSettlement = true;

    [Header("房間選情結算器")]
    [SerializeField] private RoomResultCalculator roomResultCalculator;

    [Header("迷你結算 UI（可選）")]
    [SerializeField] private MiniSettlementUI miniSettlementUI;

    [Header("房間出口控制器")]
    [SerializeField] private RoomExitController roomExitController;

    [Header("獎勵面板控制器")]
    [SerializeField] private RewardPanelController rewardPanelController;

    private bool isResolvingRoomClear = false;
    private bool shouldShowRewardPanel = true;

    public void OnRoomCleared(bool showRewardPanel = true)
    {
        if (isResolvingRoomClear)
        {
            return;
        }

        shouldShowRewardPanel = showRewardPanel;

        StartCoroutine(RoomClearFlowRoutine());
    }

    private IEnumerator RoomClearFlowRoutine()
    {
        isResolvingRoomClear = true;
        PauseGameplayForSettlement();
        ForceAllVotersExit();

        int rewardedMP = 0;
        float supportRate = 0f;
        int totalVoters = 0;
        int playerSupporters = 0;

        // 只有需要顯示選情快報的房間，才做這套流程（前兩房）
        if (needMiniSettlement && roomResultCalculator != null)
        {
            supportRate = roomResultCalculator.GetSupportRate();
            totalVoters = roomResultCalculator.GetTotalVoters();
            playerSupporters = roomResultCalculator.GetPlayerSupporters();
            rewardedMP = roomResultCalculator.CalculateAndRewardMP();
        }

        // 前兩房顯示選情快報
        if (needMiniSettlement)
        {
            if (miniSettlementUI == null)
            {
                miniSettlementUI = FindFirstObjectByType<MiniSettlementUI>(FindObjectsInactive.Include);
            }

            if (miniSettlementUI != null)
            {
                yield return StartCoroutine(
                    miniSettlementUI.ShowSettlementThenContinue(
                        supportRate,
                        playerSupporters,
                        totalVoters,
                        rewardedMP,
                        null
                    )
                );
            }
            else
            {
                Debug.LogWarning("⚠️ needMiniSettlement = true，但找不到 MiniSettlementUI");
            }
        }

        if (needMiniSettlement && shouldShowRewardPanel)
        {
            ShowRewardPanel();
            yield break;
        }

        OpenExitAndFinish();
    }

    private void PauseGameplayForSettlement()
    {
        LevelTimer.Instance?.PauseTimer();
    }

    public void OnRewardSelected()
    {
        OpenExitAndFinish();
    }

    private void ShowRewardPanel()
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

        rewardPanelController.ShowRewardPanel();
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
