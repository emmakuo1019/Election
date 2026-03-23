using System.Collections;
using UnityEngine;
using TMPro;

public class VoterVisuals : MonoBehaviour
{
    [Header("泡泡元件")]
    public SpriteRenderer bubbleOutline;

    [Header("色彩設定")]
    public Color neutralColor;
    public Color playerColor;
    public Color opponentColor;

    [Header("顏色過渡")]
    public float colorTransitionDuration = 0.8f;

    private Coroutine colorCoroutine;
    private VoterLogic logic;
    
    [Header("打擊特效")]
    public ParticleSystem voteParticles;

    void Awake()
    {
        logic = GetComponent<VoterLogic>();
    }

    void OnEnable()
    {
        logic.OnPositionChanged += UpdateBubbleVisual;
    }

    void OnDisable()
    {
        logic.OnPositionChanged -= UpdateBubbleVisual;
    }
    
    // 依據立場值緩慢插值更新外框顏色。由 OnPositionChanged event 驅動。
    private void UpdateBubbleVisual(int position)
    {
        Color targetColor;
        if (position == 0)
        {
            targetColor = neutralColor;
        }
        else
        {
            float maxAbsPosition = Mathf.Max(Mathf.Abs(VoterConfig.MIN_POS), Mathf.Abs(VoterConfig.MAX_POS));
            float intensity = maxAbsPosition > 0f
                ? Mathf.Clamp01(Mathf.Abs(position) / maxAbsPosition)
                : 0f;
            Color sideColor = (position < 0) ? playerColor : opponentColor;
            targetColor = Color.Lerp(neutralColor, sideColor, intensity);
        }

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(LerpColorRoutine(targetColor));
        voteParticles?.Play();
    }

    private IEnumerator LerpColorRoutine(Color targetColor)
    {
        Color startColor = bubbleOutline.color;
        float elapsed    = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            bubbleOutline.color = Color.Lerp(startColor, targetColor, elapsed / colorTransitionDuration);
            yield return null;
        }

        bubbleOutline.color = targetColor;
        colorCoroutine = null;
    }
    
}
