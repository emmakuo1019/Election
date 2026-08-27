using UnityEngine;

/// <summary>
/// 岔路門的進入觸發器（門框大小的小範圍）。
/// 玩家走進門 → SelectOption + TriggerRoomCleared。
///
/// 解鎖條件（requiresUnlock = true 時）：
///   有獎勵流程（RewardItemSpawner 存在）→ 必須同時滿足：
///     1. OnAllEnemiesDefeated（敵人全滅）
///     2. OnRewardCollected   （玩家選完獎勵卡）
///   無獎勵流程（Survive / 無 RewardItemSpawner）→ 只需：
///     1. OnAllEnemiesDefeated  或  OnRewardCollected（誰先到都行）
///
/// 搭配 DoorPreviewZone（較大範圍）顯示任務 tip。
///
/// Hierarchy 建議：
///   Door (GameObject)
///   ├── DoorController  (此腳本 + 小 Collider，對應門框)
///   └── DoorPreviewZone (DoorPreviewZone 腳本 + 大 Collider，對應感應範圍)
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Header("門設定")]
    [Tooltip("0 = 左門（PendingOptions[0]），1 = 右門（PendingOptions[1]）")]
    [SerializeField] private int optionIndex = 0;

    [Header("門鎖")]
    [Tooltip("敵人全滅後才解鎖；false 可用於教學關或無敵人的測試場景")]
    [SerializeField] private bool requiresUnlock = true;

    [Tooltip("是否需要等玩家選完獎勵才能過門。\n" +
             "留空（Auto）時，系統自動偵測場景中是否有 RewardItemSpawner 決定。\n" +
             "若明確不需要獎勵流程可手動設為 false。")]
    [SerializeField] private bool requiresRewardBeforeUnlock = true;

    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    // 兩道解鎖旗標
    private bool _enemiesDefeated = false;
    private bool _rewardCollected = false;
    private bool _isUnlocked = false;
    private bool _hasSelected = false;  // 防重複觸發

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        if (requiresUnlock)
        {
            BattleEventManager.OnAllEnemiesDefeated += OnEnemiesDefeated;
            BattleEventManager.OnRewardCollected    += OnRewardCollected;
        }
    }

    private void OnDisable()
    {
        BattleEventManager.OnAllEnemiesDefeated -= OnEnemiesDefeated;
        BattleEventManager.OnRewardCollected    -= OnRewardCollected;
    }

    private void Start()
    {
        if (!requiresUnlock)
        {
            _isUnlocked = true;
        }

        UpdateVisual();
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>外部強制解鎖（不走雙重條件），供不需要獎勵的場景直接呼叫</summary>
    public void ForceUnlock()
    {
        _isUnlocked = true;
        UpdateVisual();
    }

    /// <summary>由 DoorPreviewZone 呼叫，顯示此門的任務 tip。</summary>
    public void ShowTip()
    {
        RoomMissionData mission = GetMission();
        string label = mission != null ? BuildLabel(mission) : "前往下一區域";
        UIManager.Instance?.ShowTutorialTipsText(label, mission?.icon);
    }

    // ── 事件回調 ─────────────────────────────────────────────────────

    private void OnEnemiesDefeated()
    {
        _enemiesDefeated = true;
        TryUnlock();
    }

    private void OnRewardCollected()
    {
        _rewardCollected = true;
        TryUnlock();
    }

    /// <summary>
    /// 根據場景是否存在 RewardItemSpawner 決定解鎖條件：
    ///   有獎勵流程 → 兩個條件都要
    ///   無獎勵流程 → 任一條件即可（敵人清空 或 時間到後的 OnRewardCollected fallback）
    /// </summary>
    private void TryUnlock()
    {
        if (_isUnlocked) return;

        bool needsReward = requiresRewardBeforeUnlock &&
                           (FindAnyObjectByType<RewardItemSpawner>() != null);

        bool canUnlock = needsReward
            ? (_enemiesDefeated && _rewardCollected)
            : (_enemiesDefeated || _rewardCollected);

        if (!canUnlock) return;

        _isUnlocked = true;
        UpdateVisual();
        Debug.Log($"[DoorController] 門 {optionIndex} 解鎖（needsReward={needsReward}）");
    }

    // ── 觸發區偵測（門框小範圍）────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TrySelect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TrySelect();
    }

    // ── 內部邏輯 ──────────────────────────────────────────────────────

    private void TrySelect()
    {
        if (!_isUnlocked || _hasSelected) return;
        _hasSelected = true;

        UIManager.Instance?.HideTutorialTips();
        GameDB.Instance?.Campaign.SelectOption(optionIndex);

        Debug.Log($"[DoorController] 玩家選擇門 {optionIndex}，觸發過關");
        BattleEventManager.TriggerRoomCleared();
    }

    private RoomMissionData GetMission()
    {
        var options = GameDB.Instance?.Campaign.PendingOptions;
        if (options == null || options.Length <= optionIndex) return null;
        return options[optionIndex].mission;
    }

    private string BuildLabel(RoomMissionData mission)
    {
        return mission.objectiveType switch
        {
            MissionObjectiveType.ReachVotePercent => $"目標：搶票 {mission.targetValue}%",
            MissionObjectiveType.Survive          => $"目標：生存 {mission.targetValue} 秒",
            MissionObjectiveType.EliminateAll     => "目標：全滅對手",
            _                                     => string.Empty,
        };
    }

    private void UpdateVisual()
    {
        if (lockedVisual   != null) lockedVisual.SetActive(!_isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(_isUnlocked);
    }
}
