using UnityEngine;

/// <summary>
/// 根據當前房間的任務類型，決定 HUD 中各元件的顯示狀態。
/// 放置在戰鬥場景（與 BattleFlowController 同層），在 Start() 時讀取一次任務並套用。
///
/// 控制規則：
///   EliminateAll → 隱藏計時器，顯示敵人計數器，顯示票數條
///   Survive      → 顯示計時器，隱藏敵人計數器，顯示票數條
///   null（無任務）→ 與 EliminateAll 相同
/// </summary>
public class MissionHUDController : MonoBehaviour
{
    [Header("HUD 元件參考（請拖拉 GameplayHUDPanel 底下的子物件）")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private GameObject enemyCounterRoot;
    [SerializeField] private GameObject voteBarRoot;

    private void Start()
    {
        ApplyHUDLayout();
    }

    /// <summary>
    /// 讀取當前 ActiveRoom.mission，套用對應的 HUD 布局。
    /// 若任務在場景載入後才確定（例如測試場景直接開啟），也可由外部手動呼叫此方法。
    /// </summary>
    public void ApplyHUDLayout()
    {
        RoomMissionData mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        MissionObjectiveType type = mission?.objectiveType ?? MissionObjectiveType.EliminateAll;

        switch (type)
        {
            case MissionObjectiveType.EliminateAll:
                SetActive(timerRoot,        false);
                SetActive(enemyCounterRoot, true);
                SetActive(voteBarRoot,      true);
                Debug.Log("[MissionHUDController] 布局：EliminateAll（顯示敵人計數，隱藏計時器）");
                break;

            case MissionObjectiveType.Survive:
                SetActive(timerRoot,        true);
                SetActive(enemyCounterRoot, false);
                SetActive(voteBarRoot,      true);
                Debug.Log("[MissionHUDController] 布局：Survive（顯示計時器，隱藏敵人計數）");
                break;

            case MissionObjectiveType.ReachVotePercent:
                // 搶票任務：兩者都顯示（計時器提供時間壓力，票數條是核心目標）
                SetActive(timerRoot,        true);
                SetActive(enemyCounterRoot, false);
                SetActive(voteBarRoot,      true);
                Debug.Log("[MissionHUDController] 布局：ReachVotePercent（顯示計時器 + 票數條）");
                break;

            default:
                SetActive(timerRoot,        false);
                SetActive(enemyCounterRoot, true);
                SetActive(voteBarRoot,      true);
                break;
        }
    }

    /// <summary>
    /// 教學模式布局：隱藏選票條、計時器、敵人計數器，只保留 HP / MP 條。
    /// 由 TutorialState 在場景載入後呼叫。
    /// </summary>
    public void ApplyTutorialLayout()
    {
        SetActive(timerRoot,        false);
        SetActive(enemyCounterRoot, false);
        SetActive(voteBarRoot,      false);
        Debug.Log("[MissionHUDController] 布局：Tutorial（隱藏選票條、計時器、敵人計數）");
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}
