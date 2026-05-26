using System.Collections;
using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1f;

    private Coroutine returnCoroutine;

    private void OnEnable()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }

        returnCoroutine = StartCoroutine(ReturnAfterDelay());
    }

    private void OnDisable()
    {
        if (returnCoroutine == null)
        {
            return;
        }

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    private IEnumerator ReturnAfterDelay()
    {
        if (lifeTime > 0f)
        {
            yield return new WaitForSeconds(lifeTime);
        }
        else
        {
            yield return null;
        }

        returnCoroutine = null;
        PoolManager.Instance.Release(gameObject);
    }
}
