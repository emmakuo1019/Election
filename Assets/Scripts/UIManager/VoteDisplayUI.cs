using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 投票顯示 UI
/// 在遊戲畫面顯示即時得票率 (你 vs 對手)
/// </summary>
public class VoteDisplayUI : MonoBehaviour
{
    [SerializeField] private Slider voteSlider;           // Slider 顯示比例
    //[SerializeField] private TMP_Text playerVotesText;    // 玩家票數
    //[SerializeField] private TMP_Text opponentVotesText;  // 對手票數
    [SerializeField] private TMP_Text playerPercentText;  // 玩家百分比
    [SerializeField] private TMP_Text opponentPercentText;// 對手百分比

    [SerializeField] private Image voteBarColor;          // Slider 顏色
    
    private Color neutralColor = Color.gray;

    private void OnEnable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnVotesChanged += OnVotesChanged;
            OnVotesChanged(GameDB.Instance.Run.PlayerVotes, GameDB.Instance.Run.OpponentVotes);
        }
    }

    private void OnDisable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnVotesChanged -= OnVotesChanged;
        }
    }

    private void OnVotesChanged(int playerVotes, int opponentVotes)
    {
        // 直接從 GameDB 取得已經封裝好的百分比
        float playerVotePercentage = GameDB.Instance.Run.PlayerVotePercentage;
        float opponentVotePercentage = 1f - playerVotePercentage;

        // 更新百分比
        if (playerPercentText != null)
            playerPercentText.text = $"{(playerVotePercentage * 100):F1}%";
        if (opponentPercentText != null)
            opponentPercentText.text = $"{(opponentVotePercentage * 100):F1}%";

        // 更新 Slider (玩家比例)
        if (voteSlider != null)
            voteSlider.value = playerVotePercentage;
    }

    public void Rebind()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnVotesChanged -= OnVotesChanged;
            GameDB.Instance.Run.OnVotesChanged += OnVotesChanged;
            OnVotesChanged(GameDB.Instance.Run.PlayerVotes, GameDB.Instance.Run.OpponentVotes);
        }
    }
}
