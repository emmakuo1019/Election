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

        Debug.Log("=== 房間已清空 / 倒數結束 ===");
        Debug.Log("目前場景：" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Debug.Log("needMiniSettlement = " + needMiniSettlement);
        Debug.Log("shouldShowRewardPanel = " + shouldShowRewardPanel);

        StartCoroutine(RoomClearFlowRoutine());
    }

    private IEnumerator RoomClearFlowRoutine()
    {
        isResolvingRoomClear = true;
        PauseGameplayForSettlement();

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

            Debug.Log($"📢 房間結算完成，額外回補 MP：{rewardedMP}");
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
        Time.timeScale = 0f;
    }

    public void OnRewardSelected()
    {
        if (!isResolvingRoomClear)
        {
            Debug.LogWarning("⚠️ RoomClearFlowController：目前沒有等待中的獎勵流程");
            return;
        }

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
        Debug.Log("🎁 選情快報結束，顯示三選一獎勵");
    }

    private void OpenExitAndFinish()
    {
        Time.timeScale = 1f;

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
        Debug.Log("🚪 房間結束流程完成，出口已開啟");
    }
}
