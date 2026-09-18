using UnityEngine;

/// <summary>
/// ExitDirectionIndicator 診斷工具 - 幫助偵錯箭頭不顯示的問題。
/// 
/// 使用方式：
///   1. 將此腳本掛在任何場景物件上（建議掛在 UIManager 上）
///   2. 按 Play 後按 F9 顯示診斷資訊
///   3. 按 F10 強制測試箭頭（指向玩家前方 10 公尺）
/// </summary>
public class ExitIndicatorDebugger : MonoBehaviour
{
    [Header("快捷鍵")]
    [Tooltip("顯示診斷資訊的按鍵")]
    [SerializeField] private KeyCode diagnoseKey = KeyCode.F9;

    [Tooltip("強制測試箭頭顯示的按鍵")]
    [SerializeField] private KeyCode testKey = KeyCode.F10;

    [Header("測試設定")]
    [Tooltip("測試箭頭時，目標距離玩家的距離")]
    [SerializeField] private float testDistance = 10f;

    private GameObject testTarget;

    private void Update()
    {
        if (Input.GetKeyDown(diagnoseKey))
        {
            DiagnoseExitIndicator();
        }

        if (Input.GetKeyDown(testKey))
        {
            TestExitIndicator();
        }
    }

    /// <summary>
    /// 診斷 ExitDirectionIndicator 的設定狀態。
    /// </summary>
    [ContextMenu("診斷 ExitDirectionIndicator")]
    public void DiagnoseExitIndicator()
    {
        Debug.Log("========== ExitDirectionIndicator 診斷 ==========");

        // 1. 檢查 UIManager
        if (UIManager.Instance == null)
        {
            Debug.LogError("✗ UIManager.Instance 為 null！");
            return;
        }
        Debug.Log("✓ UIManager.Instance 存在");

        // 2. 檢查 ExitDirectionIndicator
        ExitDirectionIndicator indicator = UIManager.Instance.GetExitDirectionIndicator();
        if (indicator == null)
        {
            Debug.LogError("✗ ExitDirectionIndicator 為 null！請在 S0.unity 的 UIManager Inspector 中設定。");
            return;
        }
        Debug.Log($"✓ ExitDirectionIndicator 存在：{indicator.gameObject.name}");

        // 3. 檢查 arrowRect（透過反射讀取私有欄位）
        var arrowRectField = typeof(ExitDirectionIndicator).GetField("arrowRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (arrowRectField != null)
        {
            RectTransform arrowRect = arrowRectField.GetValue(indicator) as RectTransform;
            if (arrowRect == null)
            {
                Debug.LogError("✗ arrowRect 未設定！請在 Inspector 中指定箭頭的 RectTransform。");
            }
            else
            {
                Debug.Log($"✓ arrowRect 已設定：{arrowRect.gameObject.name}");
            }
        }

        // 4. 檢查 CanvasGroup
        var canvasGroupField = typeof(ExitDirectionIndicator).GetField("canvasGroup", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (canvasGroupField != null)
        {
            CanvasGroup canvasGroup = canvasGroupField.GetValue(indicator) as CanvasGroup;
            if (canvasGroup == null)
            {
                Debug.LogWarning("⚠ canvasGroup 未設定，將自動取得 Component。");
            }
            else
            {
                Debug.Log($"✓ canvasGroup 存在，alpha={canvasGroup.alpha:F2}");
                if (canvasGroup.alpha < 0.01f)
                {
                    Debug.LogWarning("⚠ canvasGroup.alpha 接近 0，箭頭可能看不見！");
                }
            }
        }

        // 5. 檢查 Camera.main
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("✗ Camera.main 為 null！場景中沒有標記為 'MainCamera' 的攝影機。");
        }
        else
        {
            Debug.Log($"✓ Camera.main 存在：{mainCamera.gameObject.name}");
        }

        // 6. 檢查目標
        if (indicator.HasTarget())
        {
            Transform target = indicator.GetTarget();
            Debug.Log($"✓ 有目標：{target.name} at {target.position}");
        }
        else
        {
            Debug.LogWarning("⚠ 沒有目標！請確認是否已調用 ShowExitDirectionIndicator()。");
        }

        // 7. 檢查 Canvas
        Canvas canvas = indicator.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("✗ 找不到父層 Canvas！");
        }
        else
        {
            Debug.Log($"✓ Canvas 存在：{canvas.gameObject.name}, RenderMode={canvas.renderMode}");
        }

        // 8. 檢查 GameObject 啟用狀態
        if (!indicator.gameObject.activeInHierarchy)
        {
            Debug.LogError($"✗ ExitDirectionIndicator GameObject 未啟用！{indicator.gameObject.name}");
        }
        else
        {
            Debug.Log("✓ GameObject 已啟用");
        }

        Debug.Log("================================================");
    }

    /// <summary>
    /// 強制測試箭頭顯示（創建測試目標點）。
    /// </summary>
    [ContextMenu("測試箭頭顯示")]
    public void TestExitIndicator()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ExitIndicatorDebugger] UIManager.Instance 為 null！");
            return;
        }

        ExitDirectionIndicator indicator = UIManager.Instance.GetExitDirectionIndicator();
        if (indicator == null)
        {
            Debug.LogError("[ExitIndicatorDebugger] ExitDirectionIndicator 為 null！");
            return;
        }

        // 找到玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 targetPos;

        if (player != null)
        {
            targetPos = player.transform.position + player.transform.forward * testDistance;
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                targetPos = cam.transform.position + cam.transform.forward * testDistance;
            }
            else
            {
                targetPos = new Vector3(0, 0, testDistance);
            }
        }

        // 創建測試目標
        if (testTarget == null)
        {
            testTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testTarget.name = "ExitIndicator_TestTarget";
            testTarget.GetComponent<Renderer>().material.color = Color.yellow;
        }

        testTarget.transform.position = targetPos;

        // 設定箭頭指向測試目標
        UIManager.Instance.ShowExitDirectionIndicator(testTarget.transform);

        Debug.Log($"[ExitIndicatorDebugger] 已創建測試目標：{targetPos}");
        Debug.Log("[ExitIndicatorDebugger] 請觀察畫面上是否出現黃色球體和指向它的箭頭。");
        Debug.Log("[ExitIndicatorDebugger] 如果沒有箭頭，請按 F9 診斷問題。");
    }

    private void OnDestroy()
    {
        if (testTarget != null)
        {
            Destroy(testTarget);
        }
    }
}
