using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出口方向指示器 - 持續顯示在畫面上，指向出口的方向。
/// 類似導航箭頭，幫助玩家找到出口位置。
/// 
/// Prefab 結構建議：
///   ExitIndicatorPanel (Canvas 子物件)
///   ├── Arrow (Image) - 箭頭圖示，會旋轉指向出口
///   └── DistanceText (TMP_Text, 可選) - 顯示距離
/// 
/// 使用方式：
///   1. 在 Canvas 下創建此 UI 元素
///   2. 在 UIManager 中引用
///   3. 當出口門啟用時調用 SetTarget()
///   4. 當玩家進入門時調用 Hide()
/// </summary>
public class ExitDirectionIndicator : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;
    [SerializeField] private TMPro.TMP_Text distanceText; // 可選
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("設定")]
    [Tooltip("指示器距離螢幕邊緣的距離（像素）")]
    [SerializeField] private float edgeOffset = 100f;

    [Tooltip("是否只在目標在螢幕外時顯示")]
    [SerializeField] private bool onlyShowWhenOffScreen = false;

    [Tooltip("淡入淡出速度")]
    [SerializeField] private float fadeSpeed = 5f;

    [Header("外觀")]
    [Tooltip("箭頭顏色（可選，不設定則保持原色）")]
    [SerializeField] private Color arrowColor = Color.white;

    [Tooltip("是否顯示距離文字")]
    [SerializeField] private bool showDistance = true;

    // ── 內部狀態 ──────────────────────────────────────────────────────

    private Transform target;
    private Camera mainCamera;
    private RectTransform canvasRect;
    private bool isVisible;
    private float targetAlpha;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        // ===== 防禦性檢查 =====
        if (arrowRect == null)
        {
            Debug.LogError($"[ExitDirectionIndicator] arrowRect 未設定！請在 Inspector 中指定箭頭的 RectTransform。GameObject: {gameObject.name}", this);
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 獲取 Canvas 的 RectTransform
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
        else
            Debug.LogWarning($"[ExitDirectionIndicator] 找不到父層 Canvas！GameObject: {gameObject.name}", this);

        if (arrowImage != null && arrowColor != Color.white)
            arrowImage.color = arrowColor;

        if (distanceText != null)
            distanceText.gameObject.SetActive(showDistance);
    }

    private void Update()
    {
        if (target == null || canvasRect == null)
        {
            targetAlpha = 0f;
        }
        else
        {
            UpdateIndicator();
        }

        // 平滑淡入淡出
        if (canvasGroup != null)
        {
            float oldAlpha = canvasGroup.alpha;
            canvasGroup.alpha = Mathf.Lerp(
                canvasGroup.alpha,
                targetAlpha,
                Time.unscaledDeltaTime * fadeSpeed
            );
            
            // ===== 除錯日誌（每秒最多一次）=====
            if (Time.frameCount % 60 == 0 && target != null)
            {
                Debug.Log($"[ExitDirectionIndicator] alpha: {oldAlpha:F2} → {canvasGroup.alpha:F2}, target: {targetAlpha:F2}, hasTarget: {target != null}, hasCamera: {mainCamera != null}");
            }
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 設定要指向的目標（通常是出口門的 Transform）。
    /// </summary>
    public void SetTarget(Transform exitTarget)
    {
        target = exitTarget;
        mainCamera = Camera.main;

        // ===== 防禦性檢查 =====
        if (mainCamera == null)
        {
            Debug.LogError("[ExitDirectionIndicator] Camera.main 為 null！請確認場景中有標記為 'MainCamera' 的攝影機。", this);
            targetAlpha = 0f;
            isVisible = false;
            return;
        }

        if (arrowRect == null)
        {
            Debug.LogError("[ExitDirectionIndicator] arrowRect 未設定，無法顯示指示器！", this);
            targetAlpha = 0f;
            isVisible = false;
            return;
        }

        isVisible = true;
        targetAlpha = 1f;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 隱藏指示器。
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        targetAlpha = 0f;
        target = null;
    }

    /// <summary>
    /// 立即顯示指示器（如果有目標）。
    /// </summary>
    public void Show()
    {
        if (target != null)
        {
            isVisible = true;
            targetAlpha = 1f;
        }
    }

    // ── 內部邏輯 ──────────────────────────────────────────────────────

    private void UpdateIndicator()
    {
        // ===== 防禦性檢查 =====
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                targetAlpha = 0f;
                return;
            }
        }

        if (arrowRect == null)
        {
            targetAlpha = 0f;
            return;
        }

        // 計算目標在螢幕上的位置
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.position);
        
        // 檢查目標是否在螢幕內
        bool isOnScreen = targetScreenPos.z > 0 &&
                          targetScreenPos.x > 0 && targetScreenPos.x < Screen.width &&
                          targetScreenPos.y > 0 && targetScreenPos.y < Screen.height;

        // 如果設定為只在螢幕外顯示，且目標在螢幕內，則隱藏
        if (onlyShowWhenOffScreen && isOnScreen)
        {
            targetAlpha = 0f;
            return;
        }
        else
        {
            targetAlpha = 1f;
        }

        // 計算方向
        Vector3 playerPos = mainCamera.transform.position;
        Vector3 directionToTarget = (target.position - playerPos).normalized;

        // 將方向投影到螢幕平面
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        Vector3 up = mainCamera.transform.up;

        float x = Vector3.Dot(directionToTarget, right);
        float y = Vector3.Dot(directionToTarget, up);

        // 計算箭頭旋轉角度
        float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        arrowRect.localRotation = Quaternion.Euler(0, 0, angle - 90f); // -90 因為箭頭預設朝上

        // 計算箭頭在螢幕邊緣的位置
        Vector2 indicatorPosition;

        if (isOnScreen)
        {
            // 目標在螢幕內，箭頭指向目標位置
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                targetScreenPos,
                null,
                out localPoint
            );
            indicatorPosition = localPoint;
        }
        else
        {
            // 目標在螢幕外，箭頭放在螢幕邊緣
            Vector2 direction2D = new Vector2(x, y).normalized;
            
            // 計算與螢幕邊界的交點
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            
            float halfWidth = screenWidth * 0.5f - edgeOffset;
            float halfHeight = screenHeight * 0.5f - edgeOffset;

            // 計算比例，確保箭頭在邊界內
            float scaleX = halfWidth / Mathf.Abs(direction2D.x);
            float scaleY = halfHeight / Mathf.Abs(direction2D.y);
            float scale = Mathf.Min(scaleX, scaleY);

            indicatorPosition = direction2D * scale;
        }

        arrowRect.anchoredPosition = indicatorPosition;

        // 更新距離文字（如果有）
        if (distanceText != null && showDistance)
        {
            float distance = Vector3.Distance(playerPos, target.position);
            distanceText.text = $"{distance:F0}m";
        }
    }

    // ── 輔助方法 ──────────────────────────────────────────────────────

    /// <summary>
    /// 檢查是否有有效的目標。
    /// </summary>
    public bool HasTarget()
    {
        return target != null;
    }

    /// <summary>
    /// 取得當前目標。
    /// </summary>
    public Transform GetTarget()
    {
        return target;
    }
}
