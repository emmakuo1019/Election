using System;
using UnityEngine;

/// <summary>
/// 關卡計時系統
/// 玩家進入房間後開始倒計時
/// 時間結束時：禁用攻擊、選民停止動作
/// </summary>
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("計時設定")]
    [SerializeField] private float levelDuration = 120f;  // 關卡時長 (秒)

    [Header("狀態")]
    private float remainingTime;
    private bool isActive = false;
    private bool isTimeUp = false;

    // 事件
    public delegate void TimerEventDelegate();
    public delegate void TimerTickDelegate(float timeRemaining, float totalTime);

    public event TimerEventDelegate OnTimerStart;      // 計時開始
    public event TimerTickDelegate OnTimerTick;        // 每幀更新
    public event TimerEventDelegate OnTimerEnd;        // 計時結束
    public event TimerEventDelegate OnTimeUpFinal;     // 最終時間用完

    // 讀取屬性
    public float RemainingTime => remainingTime;
    public float TotalDuration => levelDuration;
    public float ProgressPercentage => remainingTime / levelDuration;
    public bool IsActive => isActive;
    public bool IsTimeUp => isTimeUp;

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
        remainingTime = levelDuration;
    }

    private void Update()
    {
        if (!isActive || isTimeUp)
            return;

        remainingTime -= Time.deltaTime;

        // 觸發每幀事件 (UI 更新用)
        OnTimerTick?.Invoke(remainingTime, levelDuration);

        // 檢查時間用完
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isTimeUp = true;
            OnTimerEnd?.Invoke();
            OnTimeUpFinal?.Invoke();

            Debug.Log("⏰ [LevelTimer] 時間結束！");
        }
    }

    /// <summary>
    /// 開始計時
    /// </summary>
    public void StartTimer()
    {
        if (isActive)
        {
            Debug.LogWarning("⚠️ 計時已在進行中");
            return;
        }

        isActive = true;
        isTimeUp = false;
        remainingTime = levelDuration;

        OnTimerStart?.Invoke();
        Debug.Log($"⏱️ [LevelTimer] 計時開始 ({levelDuration}秒)");
    }

    /// <summary>
    /// 暫停計時
    /// </summary>
    public void PauseTimer()
    {
        isActive = false;
        Debug.Log("⏸️ [LevelTimer] 計時暫停");
    }

    /// <summary>
    /// 繼續計時
    /// </summary>
    public void ResumeTimer()
    {
        if (isTimeUp)
        {
            Debug.LogWarning("⚠️ 時間已用完，無法繼續");
            return;
        }

        isActive = true;
        Debug.Log("▶️ [LevelTimer] 計時繼續");
    }

    /// <summary>
    /// 重置計時
    /// </summary>
    public void ResetTimer()
    {
        isActive = false;
        isTimeUp = false;
        remainingTime = levelDuration;
        Debug.Log("🔄 [LevelTimer] 計時已重置");
    }

    /// <summary>
    /// 格式化顯示時間 (MM:SS)
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}