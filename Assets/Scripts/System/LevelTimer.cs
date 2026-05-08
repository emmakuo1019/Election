using UnityEngine;

/// <summary>
/// 關卡計時系統
/// 每個關卡場景各自擁有一個 Timer
/// 時間結束時：通知相關系統停止功能
/// </summary>
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }
    private const float MinimumValidDuration = 0.1f;
    private const float FallbackDuration = 10f;

    [Header("計時設定")]
    [SerializeField] private float levelDuration = 120f;

    [Header("狀態")]
    [SerializeField] private float remainingTime;
    [SerializeField] private bool isActive = false;
    [SerializeField] private bool isTimeUp = false;
    [SerializeField] private RewardPanelController rewardPanelController;

    public delegate void TimerEventDelegate();
    public delegate void TimerTickDelegate(float timeRemaining, float totalTime);

    public event TimerEventDelegate OnTimerStart;
    public event TimerTickDelegate OnTimerTick;
    public event TimerEventDelegate OnTimerEnd;
    public event TimerEventDelegate OnTimeUpFinal;

    public float RemainingTime => remainingTime;
    public float TotalDuration => levelDuration;
    public float ProgressPercentage => levelDuration > 0f ? remainingTime / levelDuration : 0f;
    public bool IsActive => isActive;
    public bool IsTimeUp => isTimeUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("⚠️ 場景中有多個 LevelTimer，已刪除重複物件");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureValidDuration();
        isActive = false;
        isTimeUp = false;
        remainingTime = levelDuration;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isActive || isTimeUp)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime < 0f)
            remainingTime = 0f;

        OnTimerTick?.Invoke(remainingTime, levelDuration);

        if (remainingTime <= 0f)
        {
            isTimeUp = true;
            isActive = false;

            OnTimerEnd?.Invoke();
            OnTimeUpFinal?.Invoke();

            Debug.Log("⏰ [LevelTimer] 時間結束！");

            // 若場景已由 BattleFlowController 接手結算，就不要在這裡直接跳獎勵，
            // 避免重複顯示或在失敗時仍誤開獎勵面板。
            if (BattleFlowController.Instance == null && rewardPanelController != null)
            {
                rewardPanelController.ShowRewardPanel();
            }
        }
    }

    public void StartTimer()
    {
        if (isActive)
        {
            Debug.LogWarning("⚠️ 計時已在進行中");
            return;
        }

        EnsureValidDuration();
        isActive = true;
        isTimeUp = false;
        remainingTime = levelDuration;

        OnTimerStart?.Invoke();
        OnTimerTick?.Invoke(remainingTime, levelDuration);
    }

    private void EnsureValidDuration()
    {
        if (levelDuration >= MinimumValidDuration)
        {
            return;
        }

        Debug.LogWarning($"⚠️ [LevelTimer] levelDuration={levelDuration} 無效，改用預設 {FallbackDuration} 秒");
        levelDuration = FallbackDuration;
    }

    public void PauseTimer()
    {
        if (!isActive) return;
        isActive = false;
        Debug.Log("⏸️ [LevelTimer] 計時暫停");
    }

    public void ResumeTimer()
    {
        if (isTimeUp)
        {
            Debug.LogWarning("⚠️ 時間已用完，無法繼續");
            return;
        }

        if (isActive) return;

        isActive = true;
        Debug.Log("▶️ [LevelTimer] 計時繼續");
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
