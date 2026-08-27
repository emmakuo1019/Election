using UnityEngine;

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
        if (GameDB.Instance != null && GameDB.Instance.Run != null)
        {
            GameDB.Instance.Run.OnIntegrityHpChanged += CheckGameOver;
        }
    }

    private void OnDestroy()
    {
        if (GameDB.Instance != null && GameDB.Instance.Run != null)
        {
            GameDB.Instance.Run.OnIntegrityHpChanged -= CheckGameOver;
        }
    }

    private void CheckGameOver(float currentHp, float maxHp)
    {
        if (currentHp <= 0)
        {
            Debug.Log("[PlayerHealthSystem] 玩家誠信歸零，觸發死亡事件！");
            BattleEventManager.TriggerPlayerDied();
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;

        // 有護盾時吸收一次傷害
        if (_shieldCount > 0)
        {
            _shieldCount--;
            Debug.Log($"[PlayerHealthSystem] 誠信護盾吸收傷害！剩餘護盾: {_shieldCount}");
            return;
        }

        GameDB.Instance?.Run.ModifyIntegrityHp(-amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        GameDB.Instance?.Run.ModifyIntegrityHp(amount);
    }

    /// <summary>增加護盾層數（每層吸收一次扣除）。</summary>
    public void AddShield(int count = 1) => _shieldCount += count;

    public int ShieldCount => _shieldCount;

    private int _shieldCount = 0;
}
