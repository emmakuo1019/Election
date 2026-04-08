using UnityEngine;
using System.Collections;

public class RoomClearFlowController : MonoBehaviour
{
    [Header("這個房間結束後是否顯示獎勵面板")]
    [SerializeField] private bool needRewardPanel = true;

    [Header("獎勵面板控制器（有獎勵房才需要）")]
    [SerializeField] private RewardPanelController rewardPanelController;

    [Header("關卡流程控制器")]
    [SerializeField] private LevelFlowController levelFlowController;

    [Header("房間選情結算器")]
    [SerializeField] private RoomResultCalculator roomResultCalculator;

    [Header("迷你結算 UI（可選）")]
    [SerializeField] private MiniSettlementUI miniSettlementUI;

    private bool isResolvingRoomClear = false;

    public void OnRoomCleared()
    {
        if (isResolvingRoomClear)
        {
            return;
        }

        Debug.Log("=== 房間已清空 / 倒數結束 ===");
        Debug.Log("目前場景：" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Debug.Log("needRewardPanel = " + needRewardPanel);

        StartCoroutine(RoomClearFlowRoutine());
    }

    private IEnumerator RoomClearFlowRoutine()
    {
        isResolvingRoomClear = true;

        int rewardedMP = 0;
        float supportRate = 0f;
        int totalVoters = 0;
        int playerSupporters = 0;

        // 先做房間 MP 結算
        if (roomResultCalculator != null)
        {
            supportRate = roomResultCalculator.GetSupportRate();
            totalVoters = roomResultCalculator.GetTotalVoters();
            playerSupporters = roomResultCalculator.GetPlayerSupporters();
            rewardedMP = roomResultCalculator.CalculateAndRewardMP();

            Debug.Log($"📢 房間結算完成，額外回補 MP：{rewardedMP}");
        }
        else
        {
            Debug.LogWarning("⚠️ roomResultCalculator 沒有指定，略過房間 MP 結算");
        }

        // 如果有迷你結算 UI，就先顯示
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

        // 結算顯示完後，再決定後續流程
        ContinueRoomFlow();
        isResolvingRoomClear = false;
    }

    private void ContinueRoomFlow()
    {
        if (needRewardPanel)
        {
            if (rewardPanelController == null)
            {
                Debug.LogWarning("needRewardPanel = true，但 rewardPanelController 沒有指定");
                return;
            }

            Debug.Log("開啟獎勵面板");
            rewardPanelController.ShowRewardPanel();
        }
        else
        {
            if (levelFlowController == null)
            {
                Debug.LogWarning("levelFlowController 沒有指定");
                return;
            }

            Debug.Log("此房不顯示獎勵，直接進入下一流程");
            levelFlowController.GoToNextLevel();
        }
    }
}
