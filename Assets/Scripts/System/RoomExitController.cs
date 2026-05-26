using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomExitController : MonoBehaviour
{
    private const float DefaultVoterExitOffset = 6f;

    [Header("出口外觀（可選）")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    [Header("出口目標場景")]
    [SerializeField] private string targetSceneName;

    [Header("是否只觸發一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("特殊房間設定")]
    [SerializeField] private bool unlockOnStart = false;

    [Header("選民離場表演")]
    [SerializeField] private Transform voterExitTarget;
    [SerializeField] private float voterExitForwardOffset = DefaultVoterExitOffset;

    private bool isUnlocked = false;
    private bool hasTriggered = false;

    public string TargetSceneName => targetSceneName;

    public Vector3 GetVoterExitPosition()
    {
        if (voterExitTarget != null)
        {
            return voterExitTarget.position;
        }

        float offset = Mathf.Max(voterExitForwardOffset, DefaultVoterExitOffset);
        return transform.position + transform.forward * offset;
    }

    private void Start()
    {
        if (unlockOnStart)
        {
            isUnlocked = true;
        }

        UpdateVisual();
    }

    public void UnlockExit()
    {
        isUnlocked = true;
        hasTriggered = false;
        UpdateVisual();
    }

    public void LockExit()
    {
        isUnlocked = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked) return;
        if (triggerOnlyOnce && hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        RoomClearFlowController roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>(FindObjectsInactive.Include);
        if (roomClearFlowController != null && roomClearFlowController.HasPendingSettlement())
        {
            hasTriggered = true;
            roomClearFlowController.ShowSettlementAtExit();
            return;
        }

        hasTriggered = true;
        ProceedToNextScene();
    }

    public void ProceedToNextScene()
    {
        Time.timeScale = 1f;

        string nextSceneName = targetSceneName;
        string managedNextScene = BlockProgressManager.GetSceneAfterRoomExit();
        if (!string.IsNullOrWhiteSpace(managedNextScene))
        {
            nextSceneName = managedNextScene;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("RoomExitController：targetSceneName 沒有設定");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning("RoomExitController：無法載入場景：" + nextSceneName);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
