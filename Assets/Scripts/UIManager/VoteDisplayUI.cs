using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 投票顯示 UI
/// 在遊戲畫面顯示即時得票率 (你 vs 對手)
/// 支援兩種模式：累計票數（舊邏輯）或即時場上票數（新邏輯）
/// </summary>
public class VoteDisplayUI : MonoBehaviour
{
    [SerializeField] private Slider voteSlider;           // Slider 顯示比例
    [SerializeField] private TMP_Text playerPercentText;  // 玩家百分比
    [SerializeField] private TMP_Text opponentPercentText;// 對手百分比
    [SerializeField] private Image voteBarColor;          // Slider 顏色
    
    [Header("更新設定")]
    [SerializeField] private bool useRealtimeCount = true; // 是否使用即時統計（場上選民）
    [SerializeField] private float updateInterval = 0.2f;  // 更新間隔（秒）
    
    private Color neutralColor = Color.gray;
    private float updateTimer;

    private void OnEnable()
    {
        if (!useRealtimeCount && GameDB.Instance != null)
        {
            // 使用累計票數（舊邏輯）
            GameDB.Instance.Run.OnVotesChanged += OnVotesChanged;
            OnVotesChanged(GameDB.Instance.Run.PlayerVotes, GameDB.Instance.Run.OpponentVotes);
        }
        else if (useRealtimeCount)
        {
            // 使用即時統計，立即更新一次
            UpdateRealtimeVotes();
        }
    }

    private void OnDisable()
    {
        if (!useRealtimeCount && GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnVotesChanged -= OnVotesChanged;
        }
    }

    private void Update()
    {
        if (useRealtimeCount && GameDB.Instance != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateRealtimeVotes();
            }
        }
    }

    /// <summary>即時統計場上選民票數並更新 UI</summary>
    private void UpdateRealtimeVotes()
    {
        int playerVotes = GameDB.Instance.Run.GetCurrentPlayerVotes();
        int opponentVotes = GameDB.Instance.Run.GetCurrentOpponentVotes();
        int totalVotes = playerVotes + opponentVotes;

        float playerPercent = totalVotes > 0 ? (float)playerVotes / totalVotes : 0f;
        float opponentPercent = 1f - playerPercent;

        UpdateUI(playerPercent, opponentPercent);
    }

    /// <summary>使用累計票數更新 UI（舊邏輯）</summary>
    private void OnVotesChanged(int playerVotes, int opponentVotes)
    {
        float playerVotePercentage = GameDB.Instance.Run.PlayerVotePercentage;
        float opponentVotePercentage = 1f - playerVotePercentage;

        UpdateUI(playerVotePercentage, opponentVotePercentage);
    }

    /// <summary>更新 UI 顯示</summary>
    private void UpdateUI(float playerPercent, float opponentPercent)
    {
        if (playerPercentText != null)
            playerPercentText.text = $"{(playerPercent * 100):F1}%";
        if (opponentPercentText != null)
            opponentPercentText.text = $"{(opponentPercent * 100):F1}%";
        if (voteSlider != null)
            voteSlider.value = playerPercent;
    }

    public void Rebind()
    {
        if (GameDB.Instance != null)
        {
            if (!useRealtimeCount)
            {
                GameDB.Instance.Run.OnVotesChanged -= OnVotesChanged;
                GameDB.Instance.Run.OnVotesChanged += OnVotesChanged;
                OnVotesChanged(GameDB.Instance.Run.PlayerVotes, GameDB.Instance.Run.OpponentVotes);
            }
            else
            {
                UpdateRealtimeVotes();
            }
        }
    }
}
