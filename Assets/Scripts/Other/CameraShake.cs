// /Assets/Scripts/Player/CameraShake.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 畫面震動，掛在 Main Camera 上。
/// 因 Camera 是 Player 子物件，震動用 localPosition offset 實作，
/// 不影響跟隨邏輯。
/// 呼叫：CameraShake.Instance.Shake();
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("預設參數")]
    public float defaultIntensity = 0.15f;
    public float defaultDuration  = 0.2f;

    private Vector3    originalLocalPos;
    private Coroutine  shakeCoroutine;

    void Awake()
    {
        Instance        = this;
        originalLocalPos = transform.localPosition;
    }

    /// <summary>觸發畫面震動。</summary>
    public void Shake(float intensity = -1f, float duration = -1f)
    {
        float i = intensity < 0f ? defaultIntensity : intensity;
        float d = duration  < 0f ? defaultDuration  : duration;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(i, d));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 強度隨時間 ease-out 衰減
            float current = Mathf.Lerp(intensity, 0f, elapsed / duration);
            transform.localPosition = originalLocalPos + Random.insideUnitSphere * current;
            yield return null;
        }
        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}