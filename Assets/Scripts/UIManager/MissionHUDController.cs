using UnityEngine;

/// <summary>
/// 根據當前房間任務的 HUDLayout 設定，決定 HUD 中各元件的顯示狀態。
///
/// 掛載位置：放在 gameplayHUDPanel（DontDestroyOnLoad 的 UIManager 子物件）底下，
/// 並在 Inspector 把同層的 timerRoot / enemyCounterRoot / voteBarRoot 拖進來。
/// 這樣可以確保引用永遠有效，不會有跨場景引用失效的問題。
///
/// 控制規則（HUDLayout.Auto 時依 objectiveType 決定）：
///   EliminateAll     → 計時器隱藏，敵人計數顯示，票數條顯示
///   Survive          → 計時器顯示，敵人計數隱藏，票數條顯示
///   ReachVotePercent → 計時器顯示，敵人計數隱藏，票數條顯示
///   Tutorial / HideAll → 全部隱藏，只保留 HP / MP
/// </summary>
public class MissionHUDController : MonoBehaviour
{
    [Header("HUD 子元件（從 gameplayHUDPanel 底下拖入）")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private GameObject enemyCounterRoot;
    [SerializeField] private GameObject voteBarRoot;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Start()
    {
        // 不在 Start() 時呼叫 ApplyHUDLayout()
        // 原因：此時 GameDB.Instance.Campaign.ActiveRoom 可能尚未載入
        // UIManager.ShowGameplayHUD() 會在適當時機呼叫 ApplyHUDLayout()
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 讀取當前 ActiveRoom.mission.hudLayout，套用對應的 HUD 布局。
    /// 由 UIManager.ShowGameplayHUD() 在每次顯示 HUD 後呼叫。
    /// </summary>
    public void ApplyHUDLayout()
    {
        RoomMissionData mission = GameDB.Instance?.Campaign?.ActiveRoom.mission;
        if (mission == null)
        {
            SetHUD(timer: false, enemy: true, vote: true); // 預設為 EliminateAll 布局
            return;
        }

        HUDLayout layout = mission.hudLayout;

        switch (layout)
        {
            case HUDLayout.Tutorial:
            case HUDLayout.HideAll:
                SetHUD(timer: false, enemy: false, vote: false);
                return;

            case HUDLayout.EnemyCountOnly:
                SetHUD(timer: false, enemy: true, vote: false);
                return;

            case HUDLayout.TimerAndVote:
                SetHUD(timer: true, enemy: false, vote: true);
                return;

            case HUDLayout.Auto:
            default:
                break;
        }

        // Auto：依 objectiveType 決定
        MissionObjectiveType type = mission.objectiveType;
        switch (type)
        {
            case MissionObjectiveType.EliminateAll:
                SetHUD(timer: false, enemy: true, vote: true);
                break;

            case MissionObjectiveType.Survive:
                SetHUD(timer: true, enemy: false, vote: true);
                break;

            case MissionObjectiveType.ReachVotePercent:
                SetHUD(timer: true, enemy: false, vote: true);
                break;

            default:
                SetHUD(timer: false, enemy: true, vote: true);
                break;
        }
    }

    // ── 內部 ──────────────────────────────────────────────────────────

    private void SetHUD(bool timer, bool enemy, bool vote)
    {
        if (timerRoot        != null) timerRoot.SetActive(timer);
        if (enemyCounterRoot != null) enemyCounterRoot.SetActive(enemy);
        if (voteBarRoot      != null) voteBarRoot.SetActive(vote);
    }
}
