using UnityEngine;

/// <summary>
/// 玩家誠信 HP 與護盾的統一入口。
/// 掛在 S0.unity 的 GameDB GameObject（DontDestroyOnLoad），隨 GameDB 全程存活。
///
/// 生命週期：
///   Start()              → 訂閱初始 RunData
///   GameDB.ResetRunData  → 呼叫 UnsubscribeFromRun / SubscribeToRun 換訂閱對象
///   OnDestroy()          → 解除當前訂閱
/// </summary>
public class PlayerHealthSystem : MonoBehaviour
{
    public static bool HasInstance => Instance != null;
    public static PlayerHealthSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GameDB.Instance?.Run != null)
            SubscribeToRun(GameDB.Instance.Run);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameDB.Instance?.Run != null)
            UnsubscribeFromRun(GameDB.Instance.Run);
    }

    // ── 訂閱管理（供 GameDB.ResetRunData 呼叫）──────────────────────

    public void SubscribeToRun(RunData run)
    {
        if (run == null) return;
        run.OnIntegrityHpChanged += CheckGameOver;
    }

    public void UnsubscribeFromRun(RunData run)
    {
        if (run == null) return;
        run.OnIntegrityHpChanged -= CheckGameOver;
    }

    // ── HP 與護盾 API ────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;

        if (_shieldCount > 0)
        {
            _shieldCount--;
            Debug.Log($"[PlayerHealthSystem] 誠信護盾吸收傷害！剩餘護盾：{_shieldCount}");
            return;
        }

        GameDB.Instance?.Run.ModifyIntegrityHp(-amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        GameDB.Instance?.Run.ModifyIntegrityHp(amount);
    }

    /// <summary>增加護盾層數（每層吸收一次傷害）。</summary>
    public void AddShield(int count = 1) => _shieldCount += count;

    /// <summary>新局開始時重置護盾，由 HQState 或 BootState 呼叫。</summary>
    public void ResetShield() => _shieldCount = 0;

    public int ShieldCount => _shieldCount;

    // ── 內部 ─────────────────────────────────────────────────────────

    private int _shieldCount = 0;

    private void CheckGameOver(float currentHp, float maxHp)
    {
        if (currentHp <= 0)
        {
            Debug.Log("[PlayerHealthSystem] 玩家誠信歸零，觸發死亡事件！");
            BattleEventManager.TriggerPlayerDied();
        }
    }
}
