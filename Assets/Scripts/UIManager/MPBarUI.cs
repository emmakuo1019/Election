using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MPBarUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private TMP_Text mpText;

    private void OnEnable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnMPChanged += UpdateUI;
            UpdateUI(GameDB.Instance.Run.CurrentMP, GameDB.Instance.Run.MaxMP);
        }
    }

    private void OnDisable()
    {
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnMPChanged -= UpdateUI;
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
        if (GameDB.Instance != null)
        {
            GameDB.Instance.Run.OnMPChanged -= UpdateUI;
            GameDB.Instance.Run.OnMPChanged += UpdateUI;
            UpdateUI(GameDB.Instance.Run.CurrentMP, GameDB.Instance.Run.MaxMP);
        }
    }
}
