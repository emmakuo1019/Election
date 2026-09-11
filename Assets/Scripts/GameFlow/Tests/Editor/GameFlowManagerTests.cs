#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 戰役規則測試刻意不載入場景：它們驗證唯一的 CampaignData 來源，
/// 因此不會被 UI、計時器或 Unity 的載入時序干擾。
/// </summary>
public class GameFlowManagerTests
{
    private readonly List<Object> _createdAssets = new List<Object>();

    [Test]
    public void Campaign_EliteIsNodeSix_AndFinalBossIsTheOnlyRunEnd()
    {
        CampaignDefinition definition = CreateDefinition();
        MissionPool pool = CreatePool();
        var campaign = new CampaignData(definition, pool);

        Assert.That(campaign.StartFormalCampaign(), Is.True);
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(0));  // 準備進入節點 1
        Assert.That(campaign.CurrentRole, Is.EqualTo(EncounterNodeRole.MissionChoice));
        Assert.That(campaign.PendingOptions, Has.Length.EqualTo(2));  // 已生成選項

        // 選擇路線 0，進入節點 1
        Assert.That(campaign.TrySelectRoute(0), Is.True);
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(1));

        ResolveAndChoose(campaign); // 1 -> choose 2
        ResolveAndChoose(campaign); // 2 -> choose 3
        ResolveAndChoose(campaign); // 3 -> choose 4
        ResolveAndChoose(campaign); // 4 -> choose 5

        campaign.ResolveCurrentEncounter(EncounterOutcome.Success);
        Assert.That(campaign.TryPrepareNextStep(out bool hasChoiceAtElite, out bool endedAtElite), Is.True);
        Assert.That(hasChoiceAtElite, Is.False);
        Assert.That(endedAtElite, Is.False);
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(6));
        Assert.That(campaign.CurrentRole, Is.EqualTo(EncounterNodeRole.Elite));

        // Elite 的結算依然生成第 7 節點的雙選項，不能結束這一局。
        campaign.ResolveCurrentEncounter(EncounterOutcome.Success);
        Assert.That(campaign.TryPrepareNextStep(out bool hasChoiceAfterElite, out bool endedAfterElite), Is.True);
        Assert.That(hasChoiceAfterElite, Is.True);
        Assert.That(endedAfterElite, Is.False);
        Assert.That(campaign.PendingOptions, Has.Length.EqualTo(2));
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(6));

        Assert.That(campaign.TrySelectRoute(0), Is.True); // 7
        ResolveAndChoose(campaign); // 7 -> choose 8
        ResolveAndChoose(campaign); // 8 -> choose 9
        ResolveAndChoose(campaign); // 9 -> choose 10
        ResolveAndChoose(campaign); // 10 -> choose 11

        campaign.ResolveCurrentEncounter(EncounterOutcome.Failed);
        Assert.That(campaign.TryPrepareNextStep(out bool hasChoiceBeforeFinal, out bool endedBeforeFinal), Is.True);
        Assert.That(hasChoiceBeforeFinal, Is.False);
        Assert.That(endedBeforeFinal, Is.False);
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(12));
        Assert.That(campaign.CurrentRole, Is.EqualTo(EncounterNodeRole.FinalBoss));

        campaign.ResolveCurrentEncounter(EncounterOutcome.Success);
        Assert.That(campaign.TryPrepareNextStep(out bool hasChoiceAfterFinal, out bool endedAfterFinal), Is.True);
        Assert.That(hasChoiceAfterFinal, Is.False);
        Assert.That(endedAfterFinal, Is.True);
        Assert.That(campaign.Results, Has.Count.EqualTo(12));
    }

    [Test]
    public void Campaign_NodeOneIsAlsoMissionChoice_AndReturnsValidPendingOptions()
    {
        CampaignDefinition definition = CreateDefinition();
        MissionPool pool = CreatePool();
        var campaign = new CampaignData(definition, pool);

        // 啟動戰役時，節點 1 是 MissionChoice
        Assert.That(campaign.StartFormalCampaign(), Is.True);
        
        // 應該生成 2 個任務選項
        Assert.That(campaign.PendingOptions, Has.Length.EqualTo(2));
        Assert.That(campaign.PendingOptions[0].mission, Is.Not.Null);
        Assert.That(campaign.PendingOptions[1].mission, Is.Not.Null);
        
        // CurrentNodeNumber 應為 0（準備進入節點 1）
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(0));
        Assert.That(campaign.CurrentRole, Is.EqualTo(EncounterNodeRole.MissionChoice));
        
        // 記錄選擇前的任務引用
        RoomMissionData selectedMission = campaign.PendingOptions[0].mission;
        
        // 玩家選擇其中一個路線
        Assert.That(campaign.TrySelectRoute(0), Is.True);
        
        // 選擇後 CurrentNodeNumber 應為 1
        Assert.That(campaign.CurrentNodeNumber, Is.EqualTo(1));
        Assert.That(campaign.ActiveRoom.mission, Is.SameAs(selectedMission));
    }

    [Test]
    public void Campaign_SameSeedProducesTheSamePendingMissionOptions()
    {
        CampaignDefinition definition = CreateDefinition();
        MissionPool pool = CreatePool();
        var first = new CampaignData(definition, pool);
        var second = new CampaignData(definition, pool);

        Assert.That(first.StartFormalCampaign(), Is.True);
        Assert.That(second.StartFormalCampaign(), Is.True);
        first.ResolveCurrentEncounter(EncounterOutcome.Success);
        second.ResolveCurrentEncounter(EncounterOutcome.Success);

        Assert.That(first.TryPrepareNextStep(out bool firstNeedsChoice, out _), Is.True);
        Assert.That(second.TryPrepareNextStep(out bool secondNeedsChoice, out _), Is.True);
        Assert.That(firstNeedsChoice && secondNeedsChoice, Is.True);
        Assert.That(first.PendingOptions, Has.Length.EqualTo(2));
        Assert.That(second.PendingOptions, Has.Length.EqualTo(2));
        Assert.That(first.PendingOptions[0].mission, Is.SameAs(second.PendingOptions[0].mission));
        Assert.That(first.PendingOptions[1].mission, Is.SameAs(second.PendingOptions[1].mission));

        // 重複請求不應重抽：選項在 CampaignData 中只生成一次。
        RoomMissionData firstOption = first.PendingOptions[0].mission;
        Assert.That(first.TryPrepareNextStep(out bool repeatedNeedsChoice, out _), Is.True);
        Assert.That(repeatedNeedsChoice, Is.True);
        Assert.That(first.PendingOptions[0].mission, Is.SameAs(firstOption));
    }

    [Test]
    public void FinalBossOpponent_OnlyReportsVictoryWhenItsHealthReachesZero()
    {
        GameObject bossObject = Track(new GameObject("FinalBossOpponent"));
        bossObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
        EnemyController boss = bossObject.AddComponent<EnemyController>();
        SetPrivateField(boss, "isFinalBossOpponent", true);

        int defeatedCount = 0;
        System.Action handler = () => defeatedCount++;
        BattleEventManager.OnFinalBossDefeated += handler;
        try
        {
            boss.TakeDamage(boss.maxHP - 1);
            Assert.That(defeatedCount, Is.Zero);

            boss.TakeDamage(1);
            Assert.That(defeatedCount, Is.EqualTo(1));
        }
        finally
        {
            BattleEventManager.OnFinalBossDefeated -= handler;
        }
    }

    private void ResolveAndChoose(CampaignData campaign)
    {
        campaign.ResolveCurrentEncounter(EncounterOutcome.Success);
        Assert.That(campaign.TryPrepareNextStep(out bool needsChoice, out bool isRunComplete), Is.True);
        Assert.That(needsChoice, Is.True);
        Assert.That(isRunComplete, Is.False);
        Assert.That(campaign.PendingOptions, Has.Length.EqualTo(2));
        Assert.That(campaign.TrySelectRoute(0), Is.True);
    }

    private CampaignDefinition CreateDefinition()
    {
        RoomMissionData elite = CreateMission("Elite", 6, 6);
        CampaignDefinition definition = Track(ScriptableObject.CreateInstance<CampaignDefinition>());
        SetPrivateField(definition, "defaultSeed", 424242);
        SetPrivateField(definition, "routeDoorPrefab", Track(new GameObject("RouteDoorPrefab")));
        SetPrivateField(definition, "nodes", new List<CampaignNodeDefinition>
        {
            new CampaignNodeDefinition { nodeNumber = 1, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 2, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 3, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 4, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 5, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 6, role = EncounterNodeRole.Elite, fixedMission = elite },
            new CampaignNodeDefinition { nodeNumber = 7, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 8, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 9, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 10, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 11, role = EncounterNodeRole.MissionChoice },
            new CampaignNodeDefinition { nodeNumber = 12, role = EncounterNodeRole.FinalBoss, sceneName = "TestSmallBoss" },
        });
        return definition;
    }

    private MissionPool CreatePool()
    {
        MissionPool pool = Track(ScriptableObject.CreateInstance<MissionPool>());
        pool.entries = new List<MissionPool.Entry>
        {
            new MissionPool.Entry { mission = CreateMission("Eliminate", 1, 11), weight = 5 },
            new MissionPool.Entry { mission = CreateMission("Survive", 1, 11), weight = 5 },
            new MissionPool.Entry { mission = CreateMission("Vote", 1, 11), weight = 5 },
        };
        return pool;
    }

    private RoomMissionData CreateMission(string name, int minNode, int maxNode)
    {
        RoomMissionData mission = Track(ScriptableObject.CreateInstance<RoomMissionData>());
        mission.name = name;
        mission.sceneName = "TestMVP";
        mission.minCampaignNode = minNode;
        mission.maxCampaignNode = maxNode;
        return mission;
    }

    private T Track<T>(T asset) where T : Object
    {
        _createdAssets.Add(asset);
        return asset;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"找不到測試需要的欄位：{fieldName}");
        field.SetValue(target, value);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object asset in _createdAssets)
            if (asset != null) Object.DestroyImmediate(asset);
        _createdAssets.Clear();
    }
}
#endif
