using System;
using UnityEngine;

public class PlayerMPSystem : MonoBehaviour
{
    public static PlayerMPSystem Instance { get; private set; }

    [Header("MP 設定")]
    [SerializeField] private int maxMP = 100;
    [SerializeField] private int currentMP = 100;

    public event Action<int, int> OnMPChanged;

    public int MaxMP => maxMP;
    public int CurrentMP => currentMP;
    public float MPPercent => maxMP > 0 ? (float)currentMP / maxMP : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);
    }

    private void Start()
    {
        NotifyMPChanged();
    }

    public bool HasEnoughMP(int amount)
    {
        return currentMP >= amount;
    }

    public bool UseMP(int amount)
    {
        if (amount <= 0) return true;

        if (currentMP < amount)
        {
            Debug.Log("❌ MP 不足");
            return false;
        }

        currentMP -= amount;
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);

        Debug.Log($"💸 消耗 MP：-{amount} | 目前 MP：{currentMP}/{maxMP}");
        NotifyMPChanged();
        return true;
    }

    public void RecoverMP(int amount)
    {
        if (amount <= 0) return;

        int oldMP = currentMP;
        currentMP += amount;
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);

        int actualRecovered = currentMP - oldMP;

        Debug.Log($"💰 回復 MP：+{actualRecovered} | 目前 MP：{currentMP}/{maxMP}");
        NotifyMPChanged();
    }

    public void SetMaxMP(int newMaxMP, bool fillCurrent = false)
    {
        maxMP = Mathf.Max(1, newMaxMP);

        if (fillCurrent)
            currentMP = maxMP;
        else
            currentMP = Mathf.Clamp(currentMP, 0, maxMP);

        NotifyMPChanged();
    }

    private void NotifyMPChanged()
    {
        OnMPChanged?.Invoke(currentMP, maxMP);
    }
}