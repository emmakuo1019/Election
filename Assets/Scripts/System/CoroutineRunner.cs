using System.Collections;
using UnityEngine;

/// <summary>
/// 讓沒有 MonoBehaviour 基底的純 C# class 能啟動 coroutine。
/// 自動在首次使用時建立，DontDestroyOnLoad。
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    private static CoroutineRunner Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[CoroutineRunner]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
            return _instance;
        }
    }

    public static Coroutine Run(IEnumerator routine) => Instance.StartCoroutine(routine);
}
