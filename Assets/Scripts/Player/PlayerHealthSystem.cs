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
        GameDB.Instance?.Run.ModifyIntegrityHp(-amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        GameDB.Instance?.Run.ModifyIntegrityHp(amount);
    }
}
