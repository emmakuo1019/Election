using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MiniSettlementUI : MonoBehaviour
{
    [Header("面板根物件")]
    [SerializeField] private GameObject panelRoot;

    [Header("文字欄位")]
    [SerializeField] private Text supportRateText;
    [SerializeField] private Text supporterCountText;
    [SerializeField] private Text rewardMPText;

    [Header("顯示時間（秒）")]
    [SerializeField] private float displayDuration = 1.8f;

    private void Awake()
    {
        HideImmediately();
    }

    public void HideImmediately()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowSettlement(float supportRate, int playerSupporters, int totalVoters, int rewardMP)
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("MiniSettlementUI：panelRoot 沒有指定");
            return;
        }

        panelRoot.SetActive(true);

        if (supportRateText != null)
            supportRateText.text = $"支持率：{supportRate:P0}";

        if (supporterCountText != null)
            supporterCountText.text = $"支持者：{playerSupporters} / {totalVoters}";

        if (rewardMPText != null)
            rewardMPText.text = $"資金回補：+{rewardMP} MP";
    }

    public IEnumerator ShowSettlementThenContinue(float supportRate, int playerSupporters, int totalVoters, int rewardMP, System.Action onComplete)
    {
        ShowSettlement(supportRate, playerSupporters, totalVoters, rewardMP);

        yield return new WaitForSecondsRealtime(displayDuration);

        HideImmediately();

        onComplete?.Invoke();
    }
}