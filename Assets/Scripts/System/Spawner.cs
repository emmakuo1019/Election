using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Spawner : MonoBehaviour
{
    [SerializeField] private VoterConfig[] voterConfigs;
    public GameObject voterPrefab;
    public SocialAtmosphereManager climateManager;

    private const string DefaultVoterConfigFolder = "Assets/Data/Voter";

#if UNITY_EDITOR
    private void Reset()
    {
        AutoPopulateVoterConfigs();
    }

    private void OnValidate()
    {
        if (voterConfigs == null || voterConfigs.Length == 0)
        {
            AutoPopulateVoterConfigs();
        }
    }
#endif

    void Start()
    {
    }

    void Update()
    {
    }
    public void SpawnVoter(Vector3 pos)
    {
        GameObject obj = Instantiate(voterPrefab, pos, Quaternion.identity);

        VoterData data = obj.GetComponent<VoterData>();
        if (data == null)
        {
            Debug.LogWarning("生成的選民缺少 VoterData，無法套用深色選民類型。");
            return;
        }

        VoterConfig randomConfig = GetRandomVoterConfig();
        if (randomConfig != null)
        {
            data.AssignConfig(randomConfig);
        }

        if (data.Config == null || data.Config.voterType != VoterType.Dark)
        {
            float rate = climateManager != null ? climateManager.GetDarkVoterRate() : 0f;
            VoterType type = Random.value < rate ? VoterType.Dark : VoterType.Normal;
            data.ApplyType(type);
        }

        if (obj.TryGetComponent<VoterLogic>(out var voterLogic))
        {
            voterLogic.RefreshMovementSpeed();
        }

        if (obj.TryGetComponent<VoterVisuals>(out var voterVisuals))
        {
            voterVisuals.ApplyCurrentVisualState();
        }
    }

    private VoterConfig GetRandomVoterConfig()
    {
        EnsureVoterConfigsLoaded();

        if (voterConfigs == null || voterConfigs.Length == 0)
        {
            return null;
        }

        return voterConfigs[Random.Range(0, voterConfigs.Length)];
    }

    private void EnsureVoterConfigsLoaded()
    {
        if (voterConfigs != null && voterConfigs.Length > 0)
        {
            return;
        }

#if UNITY_EDITOR
        AutoPopulateVoterConfigs();
#endif
    }

#if UNITY_EDITOR
    private void AutoPopulateVoterConfigs()
    {
        string[] guids = AssetDatabase.FindAssets("t:VoterConfig", new[] { DefaultVoterConfigFolder });
        voterConfigs = new VoterConfig[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            voterConfigs[i] = AssetDatabase.LoadAssetAtPath<VoterConfig>(path);
        }
    }
#endif
}
