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

        // 1. 檢查 UIManager
        if (UIManager.Instance == null)
        {
            return;
        }

        // 2. 檢查 ExitDirectionIndicator
        ExitDirectionIndicator indicator = UIManager.Instance.GetExitDirectionIndicator();
        if (indicator == null)
        {
            return;
        }

        // 3. 檢查 arrowRect（透過反射讀取私有欄位）
        var arrowRectField = typeof(ExitDirectionIndicator).GetField("arrowRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (arrowRectField != null)
        {
            RectTransform arrowRect = arrowRectField.GetValue(indicator) as RectTransform;
            if (arrowRect == null)
            {
            }
            else
            {
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
            }
            else
            {
                if (canvasGroup.alpha < 0.01f)
                {
                }
            }
        }

        // 5. 檢查 Camera.main
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
        }
        else
        {
        }

        // 6. 檢查目標
        if (indicator.HasTarget())
        {
            Transform target = indicator.GetTarget();
        }
        else
        {
        }

        // 7. 檢查 Canvas
        Canvas canvas = indicator.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
        }
        else
        {
        }

        // 8. 檢查 GameObject 啟用狀態
        if (!indicator.gameObject.activeInHierarchy)
        {
        }
        else
        {
        }

    }

    /// <summary>
    /// 強制測試箭頭顯示（創建測試目標點）。
    /// </summary>
    [ContextMenu("測試箭頭顯示")]
    public void TestExitIndicator()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        ExitDirectionIndicator indicator = UIManager.Instance.GetExitDirectionIndicator();
        if (indicator == null)
        {
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

    }

    private void OnDestroy()
    {
        if (testTarget != null)
        {
            Destroy(testTarget);
        }
    }
}
