using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject voterPrefab;
    public SocialAtmosphereManager climateManager;

    [Header("屬性機率")]
    [SerializeField, Range(0f, 1f)] private float coldAttributeChance = 0.25f;

    public void SpawnVoter(Vector3 pos)
    {
        GameObject obj = Instantiate(voterPrefab, pos, Quaternion.identity);

        VoterData data = obj.GetComponent<VoterData>();
        if (data == null)
        {
            Debug.LogWarning("生成的選民缺少 VoterData，無法初始化新標籤邏輯。");
            return;
        }

        data.InitializeFromConfig();
        data.ConfigureIdentity(GetRandomLabel(), GetRandomLabel(), GetRandomAttribute());

        if (obj.TryGetComponent<VoterLogic>(out var voterLogic))
        {
            voterLogic.RefreshMovementSpeed();
        }

        if (obj.TryGetComponent<VoterVisuals>(out var voterVisuals))
        {
            voterVisuals.ApplyCurrentVisualState();
        }
    }

    private VoterLabel GetRandomLabel()
    {
        return Random.value < 0.5f ? VoterLabel.Rational : VoterLabel.Emotion;
    }

    private VoterAttribute GetRandomAttribute()
    {
        float darkChance = climateManager != null ? climateManager.GetDarkVoterRate() : 0f;
        float roll = Random.value;

        if (roll < darkChance)
        {
            return VoterAttribute.Dark;
        }

        if (roll < darkChance + coldAttributeChance)
        {
            return VoterAttribute.Cold;
        }

        return VoterAttribute.None;
    }
}
