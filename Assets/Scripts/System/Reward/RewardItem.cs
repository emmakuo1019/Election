using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 場景內的獎勵物件。
/// 由 RewardItemSpawner 在敵人全滅後生成，玩家靠近顯示卡牌說明，按互動鍵選擇。
///
/// Prefab 結構建議：
///   RewardItem (此腳本 + Collider Trigger)
///   ├── Base        (3D 底座 MeshRenderer)
///   └── CardSprite  (SpriteRenderer) — Billboard 自動朝向 Camera
///
/// 選擇完成後透過 OnItemSelected 事件通知 RewardItemSpawner。
/// </summary>
[RequireComponent(typeof(Collider))]
public class RewardItem : MonoBehaviour
{
    [Header("卡牌資料")]
    [SerializeField] private SpriteRenderer cardSpriteRenderer;

    [Header("互動輸入")]
    [Tooltip("對應 InputSystem 的互動按鍵 Action（例如 E 鍵）")]
    [SerializeField] private InputActionReference interactAction;

    // 由 RewardItemSpawner.Setup() 注入
    private PolicyCardData _card;
    private bool _playerInRange = false;
    private bool _selected      = false;

    /// <summary>玩家選擇此物件後觸發，傳回選擇的卡牌。</summary>
    public event System.Action<RewardItem, PolicyCardData> OnItemSelected;

    // ── 初始化 ────────────────────────────────────────────────────────

    /// <summary>由 RewardItemSpawner 呼叫，注入卡牌資料。</summary>
    public void Setup(PolicyCardData card)
    {
        _card = card;

        // 顯示卡牌圖示
        if (cardSpriteRenderer != null && card != null)
        {
            cardSpriteRenderer.sprite = card.cardArtwork;
            cardSpriteRenderer.gameObject.SetActive(true);
        }
    }

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        interactAction?.action.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action.Disable();
    }

    private void LateUpdate()
    {
        // Billboard：讓 CardSprite 永遠面向 Camera
        if (cardSpriteRenderer != null && Camera.main != null)
            cardSpriteRenderer.transform.forward = Camera.main.transform.forward;
    }

    private void Update()
    {
        if (!_playerInRange || _selected) return;

        bool interactPressed = interactAction != null
            ? interactAction.action.WasPerformedThisFrame()
            : Input.GetKeyDown(KeyCode.E);  // fallback：無 Action 時用 E 鍵

        if (interactPressed)
            Select();
    }

    // ── 觸發區偵測 ────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        UIManager.Instance?.ShowRewardDescription(_card);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        UIManager.Instance?.HideRewardDescription();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        UIManager.Instance?.ShowRewardDescription(_card);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        UIManager.Instance?.HideRewardDescription();
    }

    // ── 選擇邏輯 ─────────────────────────────────────────────────────

    private void Select()
    {
        if (_selected) return;
        _selected = true;

        UIManager.Instance?.HideRewardDescription();

        Debug.Log($"[RewardItem] 玩家選擇卡牌：{_card?.cardName}");
        OnItemSelected?.Invoke(this, _card);
    }
}
