using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MPBarUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private TMP_Text mpText;

    private void Start()
    {
        if (PlayerMPSystem.Instance == null)
        {
            Debug.LogWarning("MPBarUI：找不到 PlayerMPSystem");
            return;
        }

        PlayerMPSystem.Instance.OnMPChanged += UpdateUI;
        UpdateUI(PlayerMPSystem.Instance.CurrentMP, PlayerMPSystem.Instance.MaxMP);
    }

    private void OnDestroy()
    {
        if (PlayerMPSystem.Instance != null)
        {
            PlayerMPSystem.Instance.OnMPChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int currentMP, int maxMP)
    {
        if (mpSlider != null)
        {
            mpSlider.maxValue = maxMP;
            mpSlider.value = currentMP;
        }

        if (mpText != null)
        {
            mpText.text = $"{currentMP} / {maxMP}";
        }
    }
    public void Rebind()
    {
        if (PlayerMPSystem.Instance != null)
        {
            PlayerMPSystem.Instance.OnMPChanged -= UpdateUI;
            PlayerMPSystem.Instance.OnMPChanged += UpdateUI;
            UpdateUI(PlayerMPSystem.Instance.CurrentMP, PlayerMPSystem.Instance.MaxMP);
        }
    }
}
