using UnityEngine;
using System.Collections;

public class PooledVFXInstance : MonoBehaviour, IPoolable
{
    [Tooltip("如果大於 0，將在時間到時自動回收；否則由 ParticleSystem 的 Callback (OnParticleSystemStopped) 觸發回收")]
    public float duration = 0f;

    private Coroutine releaseCoroutine;
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            // 確保停止時會觸發 OnParticleSystemStopped 回呼
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    public void OnSpawn()
    {
        if (ps != null)
        {
            ps.Play(true);
        }

        if (duration > 0f)
        {
            if (releaseCoroutine != null)
            {
                StopCoroutine(releaseCoroutine);
            }
            releaseCoroutine = StartCoroutine(ReleaseAfterDuration());
        }
    }

    public void OnDespawn()
    {
        if (releaseCoroutine != null)
        {
            StopCoroutine(releaseCoroutine);
            releaseCoroutine = null;
        }

        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator ReleaseAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        ReleaseToPool();
    }

    private void OnParticleSystemStopped()
    {
        // 只有當 duration <= 0 時，才由 ParticleSystem 控制回收
        if (duration <= 0f)
        {
            ReleaseToPool();
        }
    }

    private void ReleaseToPool()
    {
        if (PoolManager.HasInstance)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
