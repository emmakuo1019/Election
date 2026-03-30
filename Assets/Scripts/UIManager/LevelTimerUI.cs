using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 計時 UI 顯示
/// 在遊戲畫面顯示倒計時和進度條
/// </summary>
public class LevelTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;        // 顯示 MM:SS

    private void Start()
    {
        if (LevelTimer.Instance == null)
        {
            Debug.LogError("❌ LevelTimer 未初始化！");
            enabled = false;
            return;
        }

        LevelTimer.Instance.OnTimerTick += OnTimerTick;
        LevelTimer.Instance.OnTimerEnd += OnTimerEnd;
    }

    private void OnDestroy()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerTick -= OnTimerTick;
            LevelTimer.Instance.OnTimerEnd -= OnTimerEnd;
        }
    }

    private void OnTimerTick(float timeRemaining, float totalTime)
    {
        // 更新文字 (MM:SS 格式)
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnTimerEnd()
    {
        timerText.text = "00:00";
        Debug.Log("⏰ 時間用完！");
    }
}