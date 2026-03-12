using System.Collections;
using UnityEngine;
using TMPro;

public class VoterVisuals : MonoBehaviour
{
    [Header("外框與泡泡設定")]
    public SpriteRenderer bubbleOutline;
    public SpriteRenderer bubbleBackground;
    public TextMeshPro bubbleText;
    public GameObject speechBubble;

    [Header("色彩設定")]
    public Color neutralColor = Color.black;
    public Color playerColor = new Color(0.2f, 0.4f, 1f);
    public Color opponentColor = new Color(0.1f, 0.8f, 0.2f);

    [Header("顏色過渡")]
    public float colorTransitionDuration = 0.8f;

    private Coroutine colorCoroutine;

    /// <summary>
    /// 緩慢更新外框顏色，依據 -5 到 5 的立場值插值。
    /// </summary>
    /// <param name="position">-5 到 5 的立場值</param>
    public void UpdateBubbleVisual(int position)
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
    }

    private IEnumerator LerpColorRoutine(Color targetColor)
    {
        Color startColor = bubbleOutline.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;
            bubbleOutline.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        bubbleOutline.color = targetColor;
        colorCoroutine = null;
    }

    /// <summary>
    /// 顯示轉化成功的對話框台詞。
    /// </summary>
    public void ShowConversionDialogue(string content)
    {
        speechBubble.SetActive(true);
        bubbleText.text = content;
    }
}