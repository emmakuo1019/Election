using UnityEngine;
using System.Collections;

/// <summary>
/// 音效管理器 - 統一管理所有音效和背景音樂
/// 使用 Inspector 拖拉式設計，方便組員使用
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== 背景音樂設定 ===")]
    [Tooltip("主選單和總部的背景音樂")]
    [SerializeField] private AudioClip bgmMenuAndHQ;
    
    [Tooltip("關卡任務的背景音樂")]
    [SerializeField] private AudioClip bgmMission;
    
    [Space(10)]
    [Header("=== 音效設定 ===")]
    [Tooltip("一般攻擊音效")]
    [SerializeField] private AudioClip sfxAttackNormal;
    
    [Tooltip("玩家成功轉化選民的音效")]
    [SerializeField] private AudioClip sfxConvertSuccess;
    
    [Tooltip("對手志工死亡音效")]
    [SerializeField] private AudioClip sfxEnemyVolunteerDeath;
    
    [Tooltip("對手成功轉化的音效")]
    [SerializeField] private AudioClip sfxEnemyConvertSuccess;
    
    [Tooltip("土下座技能音效")]
    [SerializeField] private AudioClip sfxSkillDogeza;

    [Space(10)]
    [Header("=== 音量設定 ===")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;
    
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.8f;

    [Space(10)]
    [Header("=== 淡入淡出設定 ===")]
    [Tooltip("背景音樂切換時的淡出/淡入時間")]
    [SerializeField] private float bgmFadeDuration = 1.5f;

    // 內部使用
    private AudioSource bgmSource;
    private AudioSource[] sfxSources;
    private int currentSFXIndex = 0;
    private const int SFX_POOL_SIZE = 8;

    private Coroutine bgmFadeCoroutine;

    private void Awake()
    {
        // Singleton 設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化音源組件
    /// </summary>
    private void InitializeAudioSources()
    {
        // 建立 BGM 音源
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        // 建立 SFX 音源池
        sfxSources = new AudioSource[SFX_POOL_SIZE];
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].loop = false;
            sfxSources[i].playOnAwake = false;
            sfxSources[i].volume = sfxVolume;
        }
    }

    #region 背景音樂控制

    /// <summary>
    /// 播放主選單/總部背景音樂
    /// </summary>
    public void PlayBGM_MenuAndHQ()
    {
        PlayBGM(bgmMenuAndHQ);
    }

    /// <summary>
    /// 播放關卡背景音樂
    /// </summary>
    public void PlayBGM_Mission()
    {
        PlayBGM(bgmMission);
    }

    /// <summary>
    /// 播放指定的背景音樂（帶淡入淡出效果）
    /// </summary>
    private void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 背景音樂為空！");
            return;
        }

        // 如果正在播放相同的音樂，不重複播放
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 停止之前的淡入淡出協程
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // 如果當前有音樂在播放，先淡出再淡入新音樂
        if (bgmSource.isPlaying)
        {
            bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clip));
        }
        else
        {
            // 直接播放並淡入
            bgmSource.clip = clip;
            bgmSource.Play();
            bgmFadeCoroutine = StartCoroutine(FadeIn(bgmSource, bgmFadeDuration));
        }
    }

    /// <summary>
    /// 交叉淡入淡出切換背景音樂
    /// </summary>
    private IEnumerator CrossFadeBGM(AudioClip newClip)
    {
        // 淡出當前音樂
        yield return StartCoroutine(FadeOut(bgmSource, bgmFadeDuration * 0.5f));

        // 切換音樂
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 淡入新音樂
        yield return StartCoroutine(FadeIn(bgmSource, bgmFadeDuration * 0.5f));

        bgmFadeCoroutine = null;
    }

    /// <summary>
    /// 停止背景音樂（淡出效果）
    /// </summary>
    public void StopBGM()
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(FadeOutAndStop(bgmSource, bgmFadeDuration));
    }

    /// <summary>
    /// 暫停背景音樂
    /// </summary>
    public void PauseBGM()
    {
        bgmSource.Pause();
    }

    /// <summary>
    /// 恢復背景音樂
    /// </summary>
    public void ResumeBGM()
    {
        bgmSource.UnPause();
    }

    #endregion

    #region 音效控制

    /// <summary>
    /// 播放一般攻擊音效
    /// </summary>
    public void PlaySFX_AttackNormal()
    {
        PlaySFX(sfxAttackNormal);
    }

    /// <summary>
    /// 播放成功轉化音效
    /// </summary>
    public void PlaySFX_ConvertSuccess()
    {
        PlaySFX(sfxConvertSuccess);
    }

    /// <summary>
    /// 播放對手志工死亡音效
    /// </summary>
    public void PlaySFX_EnemyVolunteerDeath()
    {
        PlaySFX(sfxEnemyVolunteerDeath);
    }

    /// <summary>
    /// 播放對手成功轉化音效
    /// </summary>
    public void PlaySFX_EnemyConvertSuccess()
    {
        PlaySFX(sfxEnemyConvertSuccess);
    }

    /// <summary>
    /// 播放土下座技能音效
    /// </summary>
    public void PlaySFX_SkillDogeza()
    {
        PlaySFX(sfxSkillDogeza);
    }

    /// <summary>
    /// 播放指定音效
    /// </summary>
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 音效為空！");
            return;
        }

        // 使用音源池中的下一個可用音源
        AudioSource source = sfxSources[currentSFXIndex];
        source.clip = clip;
        source.volume = sfxVolume;
        source.Play();

        // 移動到下一個音源
        currentSFXIndex = (currentSFXIndex + 1) % SFX_POOL_SIZE;
    }

    /// <summary>
    /// 在指定位置播放 3D 音效（可選功能）
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    #endregion

    #region 音量控制

    /// <summary>
    /// 設定背景音樂音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 設定音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var source in sfxSources)
        {
            source.volume = sfxVolume;
        }
    }

    /// <summary>
    /// 設定主音量（同時影響 BGM 和 SFX）
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 靜音/取消靜音背景音樂
    /// </summary>
    public void MuteBGM(bool mute)
    {
        bgmSource.mute = mute;
    }

    /// <summary>
    /// 靜音/取消靜音音效
    /// </summary>
    public void MuteSFX(bool mute)
    {
        foreach (var source in sfxSources)
        {
            source.mute = mute;
        }
    }

    #endregion

    #region 淡入淡出效果

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.volume = 0f;
    }

    private IEnumerator FadeIn(AudioSource source, float duration)
    {
        float targetVolume = bgmVolume;
        float elapsed = 0f;
        source.volume = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        yield return StartCoroutine(FadeOut(source, duration));
        source.Stop();
        source.volume = bgmVolume; // 恢復音量
    }

    #endregion

    #region 除錯功能

    /// <summary>
    /// 顯示當前音效狀態（Editor 測試用）
    /// </summary>
    [ContextMenu("顯示音效狀態")]
    private void DebugAudioStatus()
    {
        Debug.Log($"=== AudioManager 狀態 ===");
        Debug.Log($"BGM 播放中: {bgmSource.isPlaying}");
        Debug.Log($"BGM 音量: {bgmSource.volume}");
        Debug.Log($"SFX 音量: {sfxVolume}");
        Debug.Log($"當前 BGM: {(bgmSource.clip != null ? bgmSource.clip.name : "無")}");
    }

    #endregion
}
