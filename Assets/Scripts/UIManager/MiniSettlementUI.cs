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

    public void ShowSettlement(float supportRate)
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
            supporterCountText.gameObject.SetActive(false);

        if (rewardMPText != null)
            rewardMPText.gameObject.SetActive(false);
    }

    public IEnumerator ShowSettlementThenContinue(float supportRate, System.Action onComplete)
    {
        ShowSettlement(supportRate);

        yield return new WaitForSecondsRealtime(displayDuration);

        HideImmediately();

        onComplete?.Invoke();
    }
}
