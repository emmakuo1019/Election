using UnityEngine;

public class RoomClearFlowController : MonoBehaviour
{
    [Header("這個房間是否顯示獎勵面板")]
    [SerializeField] private bool needRewardPanel = true;

    [Header("獎勵面板控制器")]
    [SerializeField] private RewardPanelController rewardPanelController;

    [Header("關卡流程控制器")]
    [SerializeField] private LevelFlowController levelFlowController;


    public void OnRoomCleared()
    {
        Debug.Log("=== 房間已清空 / 倒數結束 ===");
        Debug.Log("場景：" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Debug.Log("needRewardPanel = " + needRewardPanel);

        if (needRewardPanel)
        {
            // 前兩房流程
            if (rewardPanelController == null)
            {
                Debug.LogWarning("needRewardPanel = true，但 rewardPanelController 沒指定");
                return;
            }

            Debug.Log("開啟獎勵面板（前兩房）");
            rewardPanelController.ShowRewardPanel();
        }
    }
}