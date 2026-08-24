using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵人頭頂血條，由 EnemyController 在受傷/初始化時呼叫 Refresh()。
/// </summary>
public class EnemyHPBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private Camera _cam;

    private void Awake() => _cam = Camera.main;

    private void LateUpdate()
    {
        if (_cam != null)
            transform.forward = _cam.transform.forward;
    }

    public void Refresh(int current, int max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
