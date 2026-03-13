// /Assets/Scripts/NPCScripts/VoterVisuals.cs
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 負責選民的所有視覺表現，透過訂閱 VoterLogic 的 event 驅動，
/// 與邏輯層完全解耦。
/// </summary>
public class VoterVisuals : MonoBehaviour
{
    [Header("泡泡元件")]
    public SpriteRenderer bubbleOutline;
    public SpriteRenderer bubbleBackground;
    public TextMeshPro    bubbleText;
    public GameObject     speechBubble;

    [Header("色彩設定")]
    public Color neutralColor  = Color.black;
    public Color playerColor   = new Color(0.2f, 0.4f, 1f);
    public Color opponentColor = new Color(0.1f, 0.8f, 0.2f);

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
        logic.OnConverted       += ShowConversionDialogue;
    }

    void OnDisable()
    {
        logic.OnPositionChanged -= UpdateBubbleVisual;
        logic.OnConverted       -= ShowConversionDialogue;
    }

    /// <summary>
    /// 依據立場值緩慢插值更新外框顏色。由 OnPositionChanged event 驅動。
    /// </summary>
    private void UpdateBubbleVisual(int position)
    {
        Color targetColor;
        if (position == 0)
        {
            targetColor = neutralColor;
        }
        else
        {
            float intensity = Mathf.Abs(position) / 5f;
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

    /// <summary>
    /// 顯示轉化台詞。由 OnConverted event 驅動。
    /// </summary>
    private void ShowConversionDialogue(string content)
    {
        speechBubble.SetActive(true);
        bubbleText.text = content;
    }
}
