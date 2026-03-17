using System;
using System.Collections;
using UnityEngine;

// 只凍結「有訂閱的目標」，不改 Time.timeScale，避免影響 UI/特效/全局時間。
// 用 Realtime 等待確保在凍結期間依然能準時解凍。

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    public bool IsActive { get; private set; }
    public event Action<bool> OnHitStopChanged;

    private Coroutine routine;
    private float endTimeRealtime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    // 觸發一次 Hit Stop。若目前已在凍結中，會把結束時間延後到更晚的那個。
    public void Trigger(float durationSeconds)
    {
        if (durationSeconds <= 0f) return;

        float newEnd = Time.realtimeSinceStartup + durationSeconds;
        endTimeRealtime = Mathf.Max(endTimeRealtime, newEnd);

        if (routine == null)
            routine = StartCoroutine(HitStopRoutine());
        else if (!IsActive)
            SetActive(true);
    }

    private IEnumerator HitStopRoutine()
    {
        SetActive(true);

        while (Time.realtimeSinceStartup < endTimeRealtime)
            yield return null;

        SetActive(false);
        routine = null;
    }

    private void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;
        OnHitStopChanged?.Invoke(active);
    }
}

