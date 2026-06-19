using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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

    private Coroutine colorCoroutine;
    private Coroutine hitFlashCoroutine;
    private VoterLogic logic;
    private VoterData data;

    private void Awake()
    {
        logic = GetComponent<VoterLogic>();
        data = GetComponent<VoterData>();
    }

    private void OnEnable()
    {
        if (logic != null)
        {
            logic.OnPositionChanged += UpdateBubbleVisual;
        }

        if (data != null)
        {
            data.OnIdentityChanged += OnIdentityChanged;
        }

        ApplyCurrentVisualState();
    }

    private void OnDisable()
    {
        if (logic != null)
        {
            logic.OnPositionChanged -= UpdateBubbleVisual;
        }

        if (data != null)
        {
            data.OnIdentityChanged -= OnIdentityChanged;
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
        UpdateBubbleVisual(data.currentPosition);
    }

    private void UpdateBubbleVisual(int position)
    {
        if (headRenderer == null && bodyRenderer == null)
        {
            return;
        }

        Color targetHeadColor = ResolveHeadColor(position);
        Color targetBodyColor = ResolveBodyColor(position);

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }

        colorCoroutine = StartCoroutine(LerpColorRoutine(targetHeadColor, targetBodyColor));
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
        if (position == 0)
        {
            return neutralColor;
        }

        float maxAbsPosition = Mathf.Max(Mathf.Abs(VoterConfig.MIN_POS), Mathf.Abs(VoterConfig.MAX_POS));
        float intensity = maxAbsPosition > 0f
            ? Mathf.Clamp01(Mathf.Abs(position) / maxAbsPosition)
            : 0f;
            
        // 加入最低強度保底 (例如 0.4)，只要受影響了就立刻有 40% 以上的陣營顏色，這樣視覺上才看得出來
        if (intensity > 0f)
        {
            intensity = Mathf.Lerp(0.4f, 1f, intensity);
        }

        Color sideColor = position < 0 ? opponentColor : playerColor;
        return Color.Lerp(neutralColor, sideColor, intensity);
    }

    private Color ResolveBodyColor(int position)
    {
        return ResolveHeadColor(position);
    }

    private IEnumerator LerpColorRoutine(Color targetHeadColor, Color targetBodyColor)
    {
        Color startHeadColor = headRenderer != null ? headRenderer.color : Color.white;
        Color startBodyColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;

            if (headRenderer != null)
            {
                headRenderer.color = Color.Lerp(startHeadColor, targetHeadColor, t);
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = Color.Lerp(startBodyColor, targetBodyColor, t);
            }

            yield return null;
        }

        if (headRenderer != null)
        {
            headRenderer.color = targetHeadColor;
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.color = targetBodyColor;
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
        // 強制設定為受擊顏色
        if (headRenderer != null) headRenderer.color = flashColor;
        if (bodyRenderer != null) bodyRenderer.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        // 恢復到當前陣營狀態該有的顏色
        ApplyCurrentVisualState();
        hitFlashCoroutine = null;
    }
}
