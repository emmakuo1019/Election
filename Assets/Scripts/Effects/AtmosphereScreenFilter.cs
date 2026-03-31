using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 社會風氣畫面濾鏡效果
/// 當風氣偏向情緒化時，畫面變紅/變刺眼
/// </summary>
public class AtmosphereScreenFilter : MonoBehaviour
{
    [Header("濾鏡設定")]
    [SerializeField] private Volume postProcessVolume;    // Post Processing Volume

    [SerializeField] private float emotionalIntensity = 0.5f;  // 情緒化時的濾鏡強度
    [SerializeField] private float rationalIntensity = 0f;     // 理性時的濾鏡強度

    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;

    private void Start()
    {
        if (SocialAtmosphereManager.Instance == null)
        {
            Debug.LogError("❌ SocialAtmosphereManager 未初始化！");
            enabled = false;
            return;
        }

        if (postProcessVolume == null)
        {
            Debug.LogError("❌ Post Processing Volume 未指定！");
            enabled = false;
            return;
        }

        // 獲取 Post Processing 效果
        if (!postProcessVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>();
        }

        if (!postProcessVolume.profile.TryGet<ChromaticAberration>(out chromaticAberration))
        {
            chromaticAberration = postProcessVolume.profile.Add<ChromaticAberration>();
        }

        SocialAtmosphereManager.Instance.OnAtmosphereChanged += OnAtmosphereChanged;
        RefreshFilter();
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
        RefreshFilter();
    }

    private void RefreshFilter()
    {
        SocialAtmosphereManager mgr = SocialAtmosphereManager.Instance;

        // 計算情緒化程度 (0 ~ 1)
        float emotionalLevel = Mathf.Clamp01(Mathf.Abs(mgr.SocialAtmosphere) / (float)mgr.MaxAtmosphere);

        // 當偏向情緒化時 (SocialAtmosphere < 0)
        if (mgr.IsEmotionalTendency())
        {
            // 1. 降低飽和度 + 增加紅色偏移
            colorAdjustments.saturation.value = Mathf.Lerp(0, -30f, emotionalLevel);
            
            // 2. 增加色溫（偏紅）
            colorAdjustments.colorFilter.value = Color.Lerp(Color.white, new Color(1f, 0.7f, 0.7f), emotionalLevel);

            // 3. 增加色差效果 (刺眼效果)
            chromaticAberration.intensity.value = Mathf.Lerp(0, 1f, emotionalLevel);

            Debug.Log($"🔴 情緒化濾鏡強度: {emotionalLevel}");
        }
        else if (mgr.IsRationalTendency())
        {
            // 理性時還原正常畫面
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
            chromaticAberration.intensity.value = 0f;

            Debug.Log("🟢 理性濾鏡已移除");
        }
        else
        {
            // 中立時
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
            chromaticAberration.intensity.value = 0f;
        }
    }
}