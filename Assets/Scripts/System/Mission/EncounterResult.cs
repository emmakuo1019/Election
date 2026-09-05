using System;

public enum EncounterOutcome
{
    Success,
    Failed,
}

[Serializable]
public struct EncounterResult
{
    public int nodeNumber;
    public RoomMissionData mission;
    public EncounterOutcome outcome;
    public bool usedDistressReward;

    public EncounterResult(int nodeNumber, RoomMissionData mission, EncounterOutcome outcome)
    {
        this.nodeNumber = nodeNumber;
        this.mission = mission;
        this.outcome = outcome;
        usedDistressReward = outcome == EncounterOutcome.Failed;
    }
}
