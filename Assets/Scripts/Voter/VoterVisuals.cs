using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public enum EmoteType
{
    Waver = 0,
    Success = 1,
    Lost = 2,
    Special = 3
}

public class VoterVisuals : MonoBehaviour
{
    [Header("渲染元件")]
    public SpriteRenderer bodyRenderer;
    [FormerlySerializedAs("bubbleOutline")]
    public SpriteRenderer headRenderer;

    [Header("標籤頭像")]
    [FormerlySerializedAs("rationalRationalHeadSprite")]
    public Sprite rationalHeadSprite;
    [FormerlySerializedAs("emotionEmotionHeadSprite")]
    public Sprite emotionHeadSprite;

    [Header("色彩設定")]
    public Color neutralColor;
    public Color playerColor;
    public Color opponentColor;

    [Header("顏色過渡")]
    public float colorTransitionDuration = 0.8f;

    [Header("打擊特效")]
    public ParticleSystem voteParticles;

    [Header("UI 彈出回饋")]
    [SerializeField] private SpriteRenderer emoteRenderer;
    [SerializeField] private Sprite[] emoteSprites; // 0: 問號, 1: 成功, 2: 流失

    [Header("搖擺狀態抖動")]
    public float waverShakeAmplitude = 0.05f;
    public float waverShakeFrequency = 15f;

    public Animator Anim { get; private set; }

    private Coroutine colorCoroutine;
    private Coroutine hitFlashCoroutine;
    private Coroutine waverShakeCoroutine;
    private VoterLogic logic;
    private VoterData data;

    private static readonly int HashHit = Animator.StringToHash("hit");
    private static readonly int HashCheer = Animator.StringToHash("cheer");
    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");

    private static MaterialPropertyBlock _mpb;

    private void Awake()
    {
        logic = GetComponent<VoterLogic>();
        data = GetComponent<VoterData>();
        Anim = GetComponentInChildren<Animator>();

        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (headRenderer == null) headRenderer = transform.Find("Head")?.GetComponent<SpriteRenderer>();

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    private bool isInitialized = false;

    private void OnEnable()
    {
        if (data != null)
        {
            data.OnIdentityChanged += OnIdentityChanged;
            data.OnDataUpdated += ApplyCurrentVisualState;
        }

        isInitialized = false;
        ApplyCurrentVisualState();
        isInitialized = true;
    }

    private void OnDisable()
    {
        if (data != null)
        {
            data.OnIdentityChanged -= OnIdentityChanged;
            data.OnDataUpdated -= ApplyCurrentVisualState;
        }
    }

    public void ApplyCurrentVisualState()
    {
        if (data == null)
        {
            return;
        }

        ApplyBodySprite();
        ApplyHeadSprite();
        UpdateBubbleVisual(data.CurrentPosition);
    }

    private void UpdateBubbleVisual(int position)
    {
        if (headRenderer == null && bodyRenderer == null)
        {
            return;
        }

        Color targetHeadColor = ResolveHeadColor(position);

        // 如果是剛載入的初始化階段，直接瞬間變色並跳出，不要播放特效和漸變！
        if (!isInitialized)
        {
            if (headRenderer != null) SetSpriteColor(headRenderer, targetHeadColor);
            return;
        }

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }

        colorCoroutine = StartCoroutine(LerpColorRoutine(targetHeadColor));
        voteParticles?.Play();
    }

    private void OnIdentityChanged()
    {
        ApplyCurrentVisualState();
    }

    private void ApplyHeadSprite()
    {
        if (headRenderer == null)
        {
            return;
        }

        Sprite targetSprite = ResolveHeadSprite();
        if (targetSprite != null)
        {
            headRenderer.sprite = targetSprite;
        }
    }

    private void ApplyBodySprite()
    {
        // 身體之後再接隨機精靈圖，這版先保留 prefab 既有 sprite。
    }

    private Sprite ResolveHeadSprite()
    {
        if (data == null)
        {
            return null;
        }

        return data.PrimaryLabel == VoterLabel.Emotion
            ? emotionHeadSprite
            : rationalHeadSprite;
    }

    private Color ResolveHeadColor(int position)
    {
        // 只有成功被轉化時才改變顏色，否則維持中立顏色
        if (data == null || !data.isConverted)
        {
            return neutralColor;
        }

        return data.ConvertedSide == VoterData.PlayerSideSign ? playerColor : opponentColor;
    }



    private IEnumerator LerpColorRoutine(Color targetHeadColor)
    {
        Color startHeadColor = headRenderer != null ? headRenderer.color : Color.white;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;

            if (headRenderer != null)
            {
                SetSpriteColor(headRenderer, Color.Lerp(startHeadColor, targetHeadColor, t));
            }

            yield return null;
        }

        if (headRenderer != null)
        {
            SetSpriteColor(headRenderer, targetHeadColor);
        }

        colorCoroutine = null;
    }

    public void TriggerHitFlash(Color flashColor, float duration = 0.1f)
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }
        // 重要：必須停止原本正在跑的顏色漸變，否則下一幀就會把閃爍的白色蓋掉！
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
        
        hitFlashCoroutine = StartCoroutine(HitFlashRoutine(flashColor, duration));
    }

    private IEnumerator HitFlashRoutine(Color flashColor, float duration)
    {
        // 強制設定為受擊顏色（僅頭部）
        if (headRenderer != null) SetSpriteColor(headRenderer, flashColor);
        
        yield return new WaitForSeconds(duration);
        
        // 恢復到當前陣營狀態該有的顏色
        ApplyCurrentVisualState();
        hitFlashCoroutine = null;
    }

    public void PlayHitAnimation()
    {
        if (Anim != null)
        {
            Anim.ResetTrigger(HashHit);
            Anim.SetTrigger(HashHit);
        }
    }

    public void PlayCheerAnimation()
    {
        if (Anim != null)
        {
            Anim.SetTrigger(HashCheer);
        }
    }

    public void SetMovingAnimation(bool isMoving)
    {
        if (Anim != null)
        {
            Anim.SetBool(HashIsMoving, isMoving);
        }
    }

    private void SetSpriteColor(SpriteRenderer sr, Color color)
    {
        sr.color = color;
        if (_mpb != null)
        {
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", color);
            _mpb.SetColor("_BaseColor", color); // 以防萬一也塞入 _BaseColor
            sr.SetPropertyBlock(_mpb);
        }
    }

    // ==========================================
    // 新增：UI 顯示與搖擺抖動功能
    // ==========================================

    public void ShowEmote(EmoteType type, float duration = 1.5f)
    {
        int index = (int)type;
        if (emoteRenderer == null || emoteSprites == null || index < 0 || index >= emoteSprites.Length)
        {
            return;
        }

        // 清除該物件上正在運行的任何 Tween 與延遲呼叫，避免閃爍或重疊
        DOTween.Kill(emoteRenderer.transform);

        emoteRenderer.sprite = emoteSprites[index];
        emoteRenderer.gameObject.SetActive(true);

        // 彈出動畫：從小到大，帶有回彈效果
        emoteRenderer.transform.localScale = Vector3.zero;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(emoteRenderer.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
        
        // 延遲後縮小並隱藏
        seq.AppendInterval(duration);
        seq.Append(emoteRenderer.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        seq.OnComplete(() => {
            HideEmote();
        });

        // 將 Sequence 綁定到 transform 上，這樣 DOTween.Kill(transform) 就能連同 Sequence 一起中斷
        seq.SetId(emoteRenderer.transform);
    }

    public void HideEmote()
    {
        if (emoteRenderer != null)
        {
            DOTween.Kill(emoteRenderer.transform);
            emoteRenderer.gameObject.SetActive(false);
        }
    }

    public void StartWaverShake()
    {
        if (waverShakeCoroutine != null)
        {
            StopCoroutine(waverShakeCoroutine);
        }
        waverShakeCoroutine = StartCoroutine(WaverShakeRoutine());
    }

    public void StopWaverShake()
    {
        if (waverShakeCoroutine != null)
        {
            StopCoroutine(waverShakeCoroutine);
            waverShakeCoroutine = null;
        }
        
        // 重置位置
        if (bodyRenderer != null)
        {
            bodyRenderer.transform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator WaverShakeRoutine()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;
            float offsetX = Mathf.Sin(time * waverShakeFrequency) * waverShakeAmplitude;

            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localPosition = new Vector3(offsetX, 0f, 0f);
            }

            yield return null;
        }
    }
}
