using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject voterPrefab;

    [Header("標籤機率")]
    public float rationalLabelChance = 0.5f;

    [Header("屬性機率")]
    [SerializeField, Range(0f, 1f)] private float baseColdChance = 0.0f; // 若有需要可保留基礎機率
    [SerializeField, Range(0f, 1f)] private float baseDarkChance = 0.0f;

    [Header("波次生成設定")]
    public Transform[] spawnPoints;
    public int initialSpawnCount = 30;
    public float spawnInterval = 5f;
    public int spawnPerTick = 3;

    private Coroutine spawnCoroutine;

    private void OnEnable()
    {
        BattleEventManager.OnRoomCleared += StopSpawning;
    }

    private void OnDisable()
    {
        BattleEventManager.OnRoomCleared -= StopSpawning;
    }

    private void Start()
    {
        spawnCoroutine = StartCoroutine(StartSpawningRoutine());
    }

    private void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (PoolManager.HasInstance)
        {
            PoolManager.Instance.ReleaseAllActiveObjects();
        }
    }

    public System.Collections.IEnumerator StartSpawningRoutine()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] 沒有設定生怪點 (spawnPoints)！");
            yield break;
        }

        // 初始生成
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnAtRandomPoint();
            // 快速連續生成，每生成 5 隻稍微等待一幀避免瞬間卡頓
            if (i % 5 == 0) yield return null; 
        }

        // 無窮波次生成
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            for (int i = 0; i < spawnPerTick; i++)
            {
                SpawnAtRandomPoint();
            }
        }
    }

    private void SpawnAtRandomPoint()
    {
        if (spawnPoints.Length == 0) return;
        Transform pt = spawnPoints[Random.Range(0, spawnPoints.Length)];
        // 加上微小偏移，避免選民剛生成時完全重疊
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        SpawnVoter(pt.position + offset);
    }

    public void SpawnVoter(Vector3 pos)
    {
        GameObject obj = PoolManager.Instance.Get(voterPrefab, pos, Quaternion.identity);

        if (obj == null) return;

        if (obj.TryGetComponent<VoterLogic>(out var voterLogic))
        {
            VoterLabel label = GetRandomLabel();
            VoterAttribute attribute = GetRandomAttribute();
            int stance = VoterData.NeutralSideSign;

            // 依據得票率分配深色選民立場
            if (attribute == VoterAttribute.Dark)
            {
                float playerRatio = GameDB.Instance != null ? GameDB.Instance.Run.PlayerVotePercentage : 0.5f;
                // 若 playerRatio 為 0.6f (60%)，則 Random.value < 0.6f 有 60% 機率成立
                stance = Random.value < playerRatio ? VoterData.PlayerSideSign : VoterData.EnemySideSign;
            }

            voterLogic.SetIdentity(label, attribute, stance);
        }
        else
        {
            Debug.LogWarning("生成的選民缺少 VoterLogic，無法初始化新標籤邏輯。");
        }
    }

    private VoterLabel GetRandomLabel()
    {
        return Random.value < rationalLabelChance ? VoterLabel.Rational : VoterLabel.Emotion;
    }

    private void CalculateDynamicProbabilities(out float darkChance, out float coldChance)
    {
        darkChance = baseDarkChance;
        coldChance = baseColdChance;

        if (GameDB.Instance == null) return;

        // 根據現有 GameDB 架構：SocialAtmosphere 介於 -100 (極端情緒) 到 +100 (極端理性)，0 為中立
        // 這裡將企劃描述的 0~100 (50為界) 完美映射至實際的 -100 ~ 100 系統中。
        int atmosphere = GameDB.Instance.Run.SocialAtmosphere;

        if (atmosphere < 0)
        {
            // 偏向情緒 (對應企劃的 50~100 區間)
            float emotionBias = Mathf.InverseLerp(0, GameDB.Instance.Run.MinAtmosphere, atmosphere);
            darkChance = Mathf.Lerp(0.1f, 0.7f, emotionBias);
        }
        else if (atmosphere > 0)
        {
            // 偏向理性 (對應企劃的 50~0 區間)
            float rationalBias = Mathf.InverseLerp(0, GameDB.Instance.Run.MaxAtmosphere, atmosphere);
            coldChance = Mathf.Lerp(0.1f, 0.5f, rationalBias);
        }
    }

    private VoterAttribute GetRandomAttribute()
    {
        CalculateDynamicProbabilities(out float darkChance, out float coldChance);

        // 依照企劃要求：優先判定深色，再判定冷感，最後是普通
        if (Random.value < darkChance)
        {
            return VoterAttribute.Dark;
        }

        if (Random.value < coldChance)
        {
            return VoterAttribute.Cold;
        }

        return VoterAttribute.None;
    }
}
