using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 劇情播放邊界。日後可替換為 Timeline/Cinemachine 的非同步實作，
/// 但不需改動關卡狀態機、任務或門的程式碼。
/// </summary>
public class NarrativeDirector : MonoBehaviour
{
    public static NarrativeDirector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 預設實作只轉送現有對話，並立即完成。日後覆寫此方法時，
    /// 在 Timeline / 運鏡 / 動畫完成後才呼叫 onComplete 即可。
    /// </summary>
    public virtual void PlayBeat(NarrativeBeat beat, Action onComplete)
    {
        if (beat != null && beat.Dialogue != null)
        {
            if (TutorialManager.Instance != null) TutorialManager.Instance.ShowDialogue(beat.Dialogue);
            else UIManager.Instance?.ShowTutorialDialogue(beat.Dialogue);
        }

        onComplete?.Invoke();
    }

    public static void Play(NarrativeBeat beat, Action onComplete)
    {
        if (beat == null)
        {
            onComplete?.Invoke();
            return;
        }

        NarrativeDirector director = Instance != null
            ? Instance
            : UnityEngine.Object.FindFirstObjectByType<NarrativeDirector>();
        if (director != null)
        {
            director.PlayBeat(beat, onComplete);
            return;
        }

        // 尚未把 Director 放入場景時，不阻塞戰役；保留現有對話作為安全 fallback。
        if (beat.Dialogue != null) UIManager.Instance?.ShowTutorialDialogue(beat.Dialogue);
        onComplete?.Invoke();
    }

    public static void PlaySequence(IReadOnlyList<NarrativeBeat> beats, Action onComplete)
    {
        PlayNext(beats, 0, onComplete);
    }

    private static void PlayNext(IReadOnlyList<NarrativeBeat> beats, int index, Action onComplete)
    {
        if (beats == null || index >= beats.Count)
        {
            onComplete?.Invoke();
            return;
        }

        Play(beats[index], () => PlayNext(beats, index + 1, onComplete));
    }
}
