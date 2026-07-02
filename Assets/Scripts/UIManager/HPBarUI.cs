using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private void OnEnable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnIntegrityHpChanged += UpdateUI;
            UpdateUI(GameDB.Instance.Run.IntegrityHp, GameDB.Instance.Run.MaxIntegrityHp);
        }
    }

    private void OnDisable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnIntegrityHpChanged -= UpdateUI;
        }
    }

    private void UpdateUI(float currentHp, float maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (hpText != null)
        {
            hpText.text = $"HP {currentHp:F0} / {maxHp:F0}";
        }
    }

    public void Rebind()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnIntegrityHpChanged -= UpdateUI;
            GameDB.Instance.Run.OnIntegrityHpChanged += UpdateUI;
            UpdateUI(GameDB.Instance.Run.IntegrityHp, GameDB.Instance.Run.MaxIntegrityHp);
        }
    }
}
