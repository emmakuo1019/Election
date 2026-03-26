using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 社會風氣 UI 顯示
/// 在遊戲畫面顯示 理性 ←→ 情感 的進度條
/// </summary>
public class SocialAtmosphereUI : MonoBehaviour
{
    [SerializeField] private Slider atmosphereSlider;
    [SerializeField] private TMP_Text atmosphereValueText;
    [SerializeField] private TMP_Text atmosphereDescText;
    [SerializeField] private Image atmosphereBarColor;

    private Color rationalColor = Color.green;
    private Color neutralColor = Color.white;
    private Color emotionalColor = Color.red;

    private void Start()
    {
        if (SocialAtmosphereManager.Instance == null)
        {
            Debug.LogError("❌ SocialAtmosphereManager 未初始化！");
            enabled = false;
            return;
        }

        if (atmosphereSlider == null || atmosphereValueText == null ||
            atmosphereDescText == null || atmosphereBarColor == null)
        {
            Debug.LogError("❌ SocialAtmosphereUI 有 UI 元件未指定！");
            enabled = false;
            return;
        }

        SocialAtmosphereManager.Instance.OnAtmosphereChanged += OnAtmosphereChanged;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (SocialAtmosphereManager.Instance != null)
        {
            SocialAtmosphereManager.Instance.OnAtmosphereChanged -= OnAtmosphereChanged;
        }
    }

    private void OnAtmosphereChanged(int oldValue, int newValue)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        SocialAtmosphereManager mgr = SocialAtmosphereManager.Instance;

        atmosphereSlider.value = mgr.AtmosphereNormalized;
        atmosphereValueText.text = $"{mgr.SocialAtmosphere} / {mgr.MaxAtmosphere}";
        atmosphereDescText.text = mgr.GetAtmosphereDescription();

        float intensity = Mathf.Abs(mgr.SocialAtmosphere) / (float)mgr.MaxAtmosphere;

        if (mgr.SocialAtmosphere > 0)
        {
            atmosphereBarColor.color = Color.Lerp(neutralColor, rationalColor, intensity);
        }
        else if (mgr.SocialAtmosphere < 0)
        {
            atmosphereBarColor.color = Color.Lerp(neutralColor, emotionalColor, intensity);
        }
        else
        {
            atmosphereBarColor.color = neutralColor;
        }
    }
}