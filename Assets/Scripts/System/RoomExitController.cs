using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomExitController : MonoBehaviour
{
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private string nextSceneName;

    private bool isUnlocked = false;

    private void Start()
    {
        UpdateVisual();
    }

    public void UnlockExit()
    {
        isUnlocked = true;
        UpdateVisual();
        Debug.Log("🚪 下一關出口已開啟");
    }

    private void UpdateVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked) return;
        if (!other.CompareTag("Player")) return;

        SceneManager.LoadScene(nextSceneName);
    }
}