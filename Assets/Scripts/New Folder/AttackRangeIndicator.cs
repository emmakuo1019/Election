using System.Collections;
using UnityEngine;

/// <summary>
/// 使用 LineRenderer 在地面繪製扇形或圓形攻擊範圍指示器。
/// 可掛在玩家或 Boss 上，透過 Show/Hide 控制顯示。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AttackRangeIndicator : MonoBehaviour
{
    [Header("形狀")]
    public float range = 3f;
    [Range(0f, 360f)]
    public float angle = 60f;      // 設為 360 = 全圓 (適合 Boss AOE)
    public int segments = 40;      // 越高越平滑

    [Header("外觀")]
    public Material lineMaterial;
    public Color idleColor = new Color(1f, 1f, 1f, 0.2f);
    public Color activeColor = new Color(1f, 0.3f, 0.1f, 0.9f);
    public float lineWidth = 0.06f;
    public float heightOffset = 0.05f;    // 離地高度，避免 Z-fighting

    [Header("閃爍（攻擊時）")]
    public bool flashOnShow = true;
    public float flashDuration = 0.15f;

    private LineRenderer lineRenderer;
    private Coroutine flashCoroutine;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        if (lineMaterial != null)
            lineRenderer.material = lineMaterial;        ApplyColor(idleColor);
        RedrawShape();
    }

    /// <summary>
    /// 更新範圍和角度後重新繪製（可在 Boss 切換技能時呼叫）。
    /// </summary>
    public void SetShape(float newRange, float newAngle)
    {
        range = newRange;
        angle = newAngle;
        RedrawShape();
    }

    /// <summary>
    /// 顯示指示器並觸發攻擊閃爍效果。
    /// </summary>
    public void Show()
    {
        lineRenderer.enabled = true;
        if (flashOnShow)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
        else
        {
            ApplyColor(activeColor);
        }
    }

    /// <summary>
    /// 隱藏指示器。
    /// </summary>
    public void Hide()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// 常態顯示（淡色），用來讓玩家隨時看見自己的攻擊範圍。
    /// </summary>
    public void ShowIdle()
    {
        lineRenderer.enabled = true;
        ApplyColor(idleColor);
    }

    // --- 內部 ---

    private void RedrawShape()
    {
        bool isFullCircle = Mathf.Approximately(angle, 360f);
        if (isFullCircle)
            DrawFullCircle();
        else
            DrawSector();
    }

    private void DrawFullCircle()
    {
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float rad = (float)i / segments * Mathf.PI * 2f;
            lineRenderer.SetPosition(i, new Vector3(
                Mathf.Sin(rad) * range,
                heightOffset,
                Mathf.Cos(rad) * range));
        }
    }

    private void DrawSector()
    {
        // 結構：原點 → 左邊半徑 → 弧 → 右邊半徑 → 回原點
        int totalPoints = segments + 3;
        lineRenderer.loop = false;
        lineRenderer.positionCount = totalPoints;

        float halfAngle = angle / 2f;

        // 原點
        lineRenderer.SetPosition(0, new Vector3(0f, heightOffset, 0f));

        // 左邊邊線起點
        float leftRad = -halfAngle * Mathf.Deg2Rad;
        lineRenderer.SetPosition(1, new Vector3(
            Mathf.Sin(leftRad) * range, heightOffset, Mathf.Cos(leftRad) * range));

        // 弧
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;
            lineRenderer.SetPosition(i + 2, new Vector3(
                Mathf.Sin(currentAngle) * range,
                heightOffset,
                Mathf.Cos(currentAngle) * range));
        }

        // 回原點收口
        lineRenderer.SetPosition(totalPoints - 1, new Vector3(0f, heightOffset, 0f));
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColor(activeColor);
        yield return new WaitForSeconds(flashDuration);
        ApplyColor(idleColor);
        flashCoroutine = null;
    }

    private void ApplyColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

#if UNITY_EDITOR
    // 在 Scene View 即時預覽形狀（不需進入 Play Mode）
    private void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
            RedrawShape();
    }
#endif
}
