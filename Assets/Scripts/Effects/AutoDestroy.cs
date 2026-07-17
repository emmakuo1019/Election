using UnityEngine;

/// <summary>
/// 自動銷毀組件，用於管理政策生成物件的生命週期，防止內存洩漏。
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [Header("生命週期")]
    [Tooltip("存活時間 (秒)")]
    public float lifetime = 5f;

    private void Start()
    {
        // 安全檢查，若 lifetime 小於或等於 0，不執行自動銷毀
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }
}
