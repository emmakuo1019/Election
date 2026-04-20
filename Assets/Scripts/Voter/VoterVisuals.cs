using System.Collections;
using UnityEngine;
using TMPro;

public class VoterVisuals : MonoBehaviour
{
    [Header("渲染元件")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer bubbleOutline;

    [Header("選民外觀")]
    public Sprite normalBodySprite;
    public Sprite emotionBodySprite;
    public Sprite coldBodySprite;
    public Sprite goodBodySprite;
    public Sprite darkBodySprite;

    [Header("色彩設定")]
    public Color neutralColor;
    public Color playerColor;
    public Color opponentColor;
    public Color darkUiColor = new(1f, 0.9f, 0.35f, 1f);

    [Header("UI 圖樣")]
    public Sprite normalUiSprite;
    public Sprite darkUiSprite;

    [Header("顏色過渡")]
    public float colorTransitionDuration = 0.8f;

    private Coroutine colorCoroutine;
    private VoterLogic logic;
    private VoterData data;
    
    [Header("打擊特效")]
    public ParticleSystem voteParticles;

    void Awake()
    {
        logic = GetComponent<VoterLogic>();
        data = GetComponent<VoterData>();
    }

    void OnEnable()
    {
        logic.OnPositionChanged += UpdateBubbleVisual;

        if (data != null)
        {
            data.OnTypeChanged += OnTypeChanged;
        }

        ApplyCurrentVisualState();
    }

    void OnDisable()
    {
        logic.OnPositionChanged -= UpdateBubbleVisual;

        if (data != null)
        {
            data.OnTypeChanged -= OnTypeChanged;
        }
    }

    public void ApplyCurrentVisualState()
    {
        if (data == null)
        {
            return;
        }

        ApplyBodySprite();
        ApplyUiSprite();
        UpdateBubbleVisual(data.currentPosition);
    }
    
    // 依據立場值緩慢插值更新外框顏色。由 OnPositionChanged event 驅動。
    private void UpdateBubbleVisual(int position)
    {
        Color targetColor = ResolveUiColor(position);
        Color targetBodyColor = ResolveBodyColor(position);

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(LerpColorRoutine(targetColor, targetBodyColor));
        voteParticles?.Play();
    }

    private void OnTypeChanged(VoterType _)
    {
        ApplyCurrentVisualState();
    }

    private void ApplyUiSprite()
    {
        if (bubbleOutline == null || data == null)
        {
            return;
        }

        bubbleOutline.sprite = data.voterType == VoterType.Dark && darkUiSprite != null
            ? darkUiSprite
            : normalUiSprite;
    }

    private void ApplyBodySprite()
    {
        if (bodyRenderer == null || data == null)
        {
            return;
        }

        Sprite targetSprite = ResolveBodySprite();
        if (targetSprite != null)
        {
            bodyRenderer.sprite = targetSprite;
        }
    }

    private Sprite ResolveBodySprite()
    {
        if (data.voterType == VoterType.Dark && darkBodySprite != null)
        {
            return darkBodySprite;
        }

        return data.Tag switch
        {
            VoterTag.emotion when emotionBodySprite != null => emotionBodySprite,
            VoterTag.cold when coldBodySprite != null => coldBodySprite,
            VoterTag.good when goodBodySprite != null => goodBodySprite,
            _ => normalBodySprite != null ? normalBodySprite : darkBodySprite
        };
    }

    private Color ResolveUiColor(int position)
    {
        if (data != null && data.voterType == VoterType.Dark)
        {
            if (data.IsPlayerAligned)
            {
                return playerColor;
            }

            if (data.IsEnemyAligned)
            {
                return opponentColor;
            }

            return darkUiColor;
        }

        if (position == 0)
        {
            return neutralColor;
        }

        return ResolveConvertedColor(position);
    }

    private Color ResolveBodyColor(int position)
    {
        if (data != null && data.voterType == VoterType.Dark)
        {
            if (data.IsPlayerAligned)
            {
                return playerColor;
            }

            if (data.IsEnemyAligned)
            {
                return opponentColor;
            }
        }

        return neutralColor;
    }

    private Color ResolveConvertedColor(int position)
    {
        float maxAbsPosition = Mathf.Max(Mathf.Abs(VoterConfig.MIN_POS), Mathf.Abs(VoterConfig.MAX_POS));
        float intensity = maxAbsPosition > 0f
            ? Mathf.Clamp01(Mathf.Abs(position) / maxAbsPosition)
            : 0f;
        Color sideColor = (position < 0) ? opponentColor : playerColor;
        return Color.Lerp(neutralColor, sideColor, intensity);
    }

    private IEnumerator LerpColorRoutine(Color targetColor, Color targetBodyColor)
    {
        Color startColor = bubbleOutline != null ? bubbleOutline.color : Color.white;
        Color startBodyColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
        float elapsed    = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;

            if (bubbleOutline != null)
            {
                bubbleOutline.color = Color.Lerp(startColor, targetColor, t);
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = Color.Lerp(startBodyColor, targetBodyColor, t);
            }

            yield return null;
        }

        if (bubbleOutline != null)
        {
            bubbleOutline.color = targetColor;
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.color = targetBodyColor;
        }

        colorCoroutine = null;
    }
    
}
