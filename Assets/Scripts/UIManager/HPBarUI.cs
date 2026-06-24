using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private void Start()
    {
        if (!PolicyEffectRuntimeManager.HasInstance)
        {
            _ = PolicyEffectRuntimeManager.Instance;
        }

        PolicyEffectRuntimeManager.Instance.OnEffectsChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        PolicyEffectRuntimeManager runtime = PolicyEffectRuntimeManager.Instance;
        if (runtime == null)
        {
            return;
        }

        if (hpSlider != null)
        {
            hpSlider.maxValue = runtime.MaxIntegrityHp;
            hpSlider.value = runtime.IntegrityHp;
        }

        if (hpText != null)
        {
            hpText.text = $"HP {runtime.IntegrityHp:F0} / {runtime.MaxIntegrityHp:F0}";
        }
    }
    public void Rebind()
    {
        if (PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged -= UpdateUI;
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged += UpdateUI;
        }
        UpdateUI();
    }
}
