using UnityEngine;
using TMPro;

/// <summary>
/// 計時 UI 顯示
/// 在遊戲畫面顯示倒計時
/// </summary>
public class LevelTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    private bool hasSubscribed = false;

    private void Start()
    {
        SubscribeToTimer();
    }

    private void OnDisable()
    {
        UnsubscribeFromTimer();
    }

    private void SubscribeToTimer()
    {
        if (hasSubscribed) return;

        if (LevelTimer.Instance == null)
        {
            Debug.LogError("❌ 場景中找不到 LevelTimer，請確認此關卡有放置 LevelTimer");
            return;
        }

        LevelTimer.Instance.OnTimerTick += OnTimerTick;
        LevelTimer.Instance.OnTimerEnd += OnTimerEnd;
        hasSubscribed = true;

        RefreshTimerDisplay();
    }

    private void UnsubscribeFromTimer()
    {
        if (!hasSubscribed) return;

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerTick -= OnTimerTick;
            LevelTimer.Instance.OnTimerEnd -= OnTimerEnd;
        }

        hasSubscribed = false;
    }

    private void OnTimerTick(float timeRemaining, float totalTime)
    {
        RefreshTimerDisplay();
    }

    private void OnTimerEnd()
    {
        if (timerText != null)
            timerText.text = "00:00";
    }

    private void RefreshTimerDisplay()
    {
        if (timerText == null || LevelTimer.Instance == null) return;

        timerText.text = LevelTimer.Instance.GetFormattedTime();
    }
}
