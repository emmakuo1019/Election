using UnityEngine;
using TMPro;

/// <summary>
/// 顯示場上剩餘敵人（或指定 Reporter）數量。
///
/// 一般 EliminateAll 任務：訂閱 EnemySpawnTracker.OnAliveCountChanged。
/// Reporter 清場任務：訂閱 ReporterMissionTracker.OnRemainingCountChanged，
///   顯示的是「指定 Reporter 剩餘數」而非全敵人數。
///
/// Hierarchy 建議：放在 GameplayHUDPanel 底下，由 MissionHUDController 控制顯隱。
/// </summary>
public class EnemyCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;
    [Tooltip("顯示格式，{0} 會被替換為剩餘數量，例如：「剩餘敵人：{0}」")]
    [SerializeField] private string displayFormat = "敵人剩餘：{0}";

    private bool _isReporterMode = false;

    private void OnEnable()
    {
        BindToCurrentMission();
    }

    private void OnDisable()
    {
        UnbindAll();
    }

    /// <summary>由 UIManager.ShowGameplayHUD() 的 Rebind 流程呼叫，確保跨場景數值正確。</summary>
    public void Rebind()
    {
        UnbindAll();
        BindToCurrentMission();
    }

    // ── 內部 ─────────────────────────────────────────────────────────

    private void BindToCurrentMission()
    {
        RoomMissionData mission = GameDB.Instance?.Campaign?.ActiveRoom?.mission;
        _isReporterMode = mission != null && mission.isReporterMission;

        if (_isReporterMode)
        {
            ReporterMissionTracker.OnRemainingCountChanged += Refresh;
            // 初始同步：registration 完成後才有數值；完成前顯示 0
            int initial = ReporterMissionTracker.Instance != null
                ? ReporterMissionTracker.Instance.RemainingCount
                : 0;
            Refresh(initial);
        }
        else
        {
            EnemySpawnTracker.OnAliveCountChanged += Refresh;
            Refresh(EnemySpawnTracker.AliveCount);
        }
    }

    private void UnbindAll()
    {
        EnemySpawnTracker.OnAliveCountChanged -= Refresh;
        ReporterMissionTracker.OnRemainingCountChanged -= Refresh;
    }

    private void Refresh(int count)
    {
        if (counterText != null)
            counterText.text = string.Format(displayFormat, count);
    }
}
