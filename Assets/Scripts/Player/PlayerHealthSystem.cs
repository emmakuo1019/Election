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
