using UnityEngine;
using TMPro;

/// <summary>
/// 顯示場上剩餘敵人數量。
/// 訂閱 EnemySpawnTracker 的靜態事件，在敵人死亡或新增時即時更新文字。
///
/// Hierarchy 建議：放在 GameplayHUDPanel 底下，由 MissionHUDController 控制顯隱。
/// </summary>
public class EnemyCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;
    [Tooltip("顯示格式，{0} 會被替換為剩餘數量，例如：「剩餘敵人：{0}」")]
    [SerializeField] private string displayFormat = "敵人剩餘：{0}";

    private void OnEnable()
    {
        EnemySpawnTracker.OnAliveCountChanged += Refresh;
        // 立即同步當前數值
        Refresh(EnemySpawnTracker.AliveCount);
    }

    private void OnDisable()
    {
        EnemySpawnTracker.OnAliveCountChanged -= Refresh;
    }

    private void Refresh(int aliveCount)
    {
        if (counterText != null)
            counterText.text = string.Format(displayFormat, aliveCount);
    }

    /// <summary>由 UIManager.ShowGameplayHUD() 的 Rebind 流程呼叫，確保跨場景數值正確</summary>
    public void Rebind()
    {
        EnemySpawnTracker.OnAliveCountChanged -= Refresh;
        EnemySpawnTracker.OnAliveCountChanged += Refresh;
        Refresh(EnemySpawnTracker.AliveCount);
    }
}
