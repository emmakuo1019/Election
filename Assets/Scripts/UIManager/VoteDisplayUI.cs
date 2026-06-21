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

    private void Start()
    {
        if (VoteManager.Instance == null)
        {
            Debug.LogError("❌ VoteManager 未初始化！");
            enabled = false;
            return;
        }

        //if (voteSlider == null || playerVotesText == null || opponentVotesText == null)
        //{
            //enabled = false;
            //return;
        //}

        VoteManager.Instance.OnVotesChanged += OnVotesChanged;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (VoteManager.Instance != null)
        {
            VoteManager.Instance.OnVotesChanged -= OnVotesChanged;
        }
    }

    private void OnVotesChanged(int playerVotes, int opponentVotes)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        VoteManager mgr = VoteManager.Instance;

        // 更新文字
        //playerVotesText.text = $"你: {mgr.PlayerVotes}";
        //opponentVotesText.text = $"對手: {mgr.OpponentVotes}";

        // 更新百分比
        playerPercentText.text = $"{(mgr.PlayerVotePercentage * 100):F1}%";
        opponentPercentText.text = $"{(mgr.OpponentVotePercentage * 100):F1}%";

        // 更新 Slider (玩家比例)
        voteSlider.value = mgr.PlayerVotePercentage;
    }
    public void Rebind()
    {
        if (VoteManager.Instance != null)
        {
            VoteManager.Instance.OnVotesChanged -= OnVotesChanged;
            VoteManager.Instance.OnVotesChanged += OnVotesChanged;
            RefreshUI();
        }
    }
}
