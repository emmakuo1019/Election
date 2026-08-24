using UnityEngine;

/// <summary>
/// 放在場景中的教學觸發區。
/// 掛上此腳本的 GameObject 需要有一個設為 Is Trigger 的 Collider。
/// 玩家進入範圍後觸發指定的教學步驟，並立即 disable 自身 Collider 避免二次觸發。
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("此觸發區對應的教學步驟資料")]
    [SerializeField] private TutorialStepData stepData;

    private Collider triggerCollider;
    private bool hasTriggered = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (stepData == null)
        {
            Debug.LogWarning($"[TutorialTrigger] {gameObject.name} 的 stepData 未設定！");
            return;
        }

        hasTriggered = true;

        // 立即關閉 Collider，避免任何二次觸發
        triggerCollider.enabled = false;

        TutorialManager.Instance?.ShowDialogue(stepData);
    }

#if UNITY_EDITOR
    // 在 Scene 視窗顯示觸發區的範圍，方便場景擺放
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }

        // 顯示步驟名稱標籤
        if (stepData != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"[教學] {stepData.advisorName}: {stepData.name}"
            );
        }
    }
#endif
}
