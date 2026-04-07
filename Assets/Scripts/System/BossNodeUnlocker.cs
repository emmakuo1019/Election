using UnityEngine;
using UnityEngine.UI;

public class BossNodeUnlocker : MonoBehaviour
{
    [Header("Boss 按鈕")]
    [SerializeField] private Button bossButton;

    [Header("未解鎖時顯示物件（可選）")]
    [SerializeField] private GameObject lockedVisual;

    [Header("已解鎖時顯示物件（可選）")]
    [SerializeField] private GameObject unlockedVisual;

    private void Start()
    {
        RefreshBossState();
    }

    public void RefreshBossState()
    {
        bool unlocked = CampaignProgressManager.IsBossUnlocked();

        if (bossButton != null)
        {
            bossButton.interactable = unlocked;
        }

        if (lockedVisual != null)
        {
            lockedVisual.SetActive(!unlocked);
        }

        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(unlocked);
        }

        Debug.Log("Boss 解鎖狀態：" + unlocked);
    }
}