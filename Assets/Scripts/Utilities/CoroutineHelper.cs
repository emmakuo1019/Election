using UnityEngine;

/// <summary>
/// 用於在靜態類別中執行協程的輔助類別。
/// </summary>
public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper _instance;

    public static CoroutineHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[CoroutineHelper]");
                _instance = go.AddComponent<CoroutineHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
