# KIRO_PROJECT_CONTEXT.md

**Project**: Election / 酸宗痛  
**Unity**: 6000.3.17f1  
**Last Verified**: 2026-10-07  
**Document Status**: Canonical Architecture Reference

---

## Before Reading This Document

This document describes **how the current codebase actually works**.

For **gameplay design rules**, see `.agents/CURRENT_DESIGN_DECISIONS.md`.  
For **permanent development rules**, see `.agents/AGENTS.md`.

---

## 1. Architecture Summary

### Core Components

**GameDB** (Single Source of Truth):
- Stores all cross-scene persistent state
- `GameDB.Campaign` (CampaignData) - Campaign progress, node number, results
- `GameDB.Run` (RunData) - Resources (Integrity HP, Funds MP, Votes), Social Atmosphere
- All numeric changes must go through GameDB methods with boundary checks
- Never store game state in Managers

**GameFlowManager** (Global State Machine):
- Manages high-level game lifecycle
- States: BootState, MainMenuState, HQState, TutorialState, GameplayState, StageClearState, GameEndState
- Owns the state machine, provides read-only `CurrentState` property
- Cross-scene persistent (DontDestroyOnLoad)

**CampaignData** (Runtime Campaign State):
- `CurrentNodeNumber` (1-12) - which node the player is on
- `CurrentRole` (MissionChoice / Elite / FinalBoss) - node type
- `ActiveRoom` - currently selected mission and scene
- `PendingOptions` - two mission options for route choice (generated once, not re-rolled)
- `Results` - List<EncounterResult> recording all node outcomes

**CampaignDefinition** (ScriptableObject):
- Defines all 12 nodes: role, fixed mission, scene, narrative beats
- Located at: `Assets/Data/Mission/CampaignDefinition.asset`
- Node roles determined by switch pattern:
  - Nodes 1-5: MissionChoice
  - Node 6: Elite
  - Nodes 7-11: MissionChoice
  - Node 12: FinalBoss

**BattleEventManager** (Static Event Hub):
- Central event dispatcher for battle scenes
- Key events: `OnObjectiveResolved`, `OnRewardCollected`, `OnRouteSelected`, `OnFinalBossDefeated`
- Tracks encounter lifecycle: Loading → Briefing → Active → ObjectiveResolved → RewardSelection → RouteSelection → Transitioning
- Each phase can only be entered once per encounter

**RoomExitController** (Scene Exit Manager):
- Manages door spawning and configuration
- `ShowExitDoors(bool needsRouteChoice)` - displays single door or double doors
- Controls door unlock conditions
- Works with DoorController components

**DoorController** (Individual Door):
- Handles player collision and route selection
- Displays mission preview information
- Triggers `BattleEventManager.TriggerRouteSelected(optionIndex)` or `TriggerExitReached()`

---

## 2. Current Campaign Architecture

### 12-Node Structure

```
Tutorial (教學)
    ↓
Node 1-5 (MissionChoice)
    ↓
Node 6 (Elite)
    ↓
Node 7-11 (MissionChoice)
    ↓
Node 12 (FinalBoss)
```

**Implementation Status**:
- Current Implementation: 12-node campaign architecture
- Code location: `CampaignDefinition.cs` with `FormalNodeCount = 12`
- Node roles determined by `GetRoleForNode(int nodeNumber)` method

**Node Role Logic**:
```csharp
public EncounterNodeRole GetRoleForNode(int nodeNumber)
{
    return nodeNumber switch
    {
        >= 1 and <= 5  => EncounterNodeRole.MissionChoice,
        6              => EncounterNodeRole.Elite,
        >= 7 and <= 11 => EncounterNodeRole.MissionChoice,
        12             => EncounterNodeRole.FinalBoss,
        _              => EncounterNodeRole.Invalid,
    };
}
```

---

## 3. GameFlow State Ownership

### State Responsibilities

**BootState**:
- Entry point, resets campaign data
- Transitions to MainMenuState

**MainMenuState**:
- Displays main menu
- Transitions to HQState on "New Game"

**HQState**:
- Headquarters scene
- Faction/character selection
- Resets RunData and CampaignData when entering from GameEndState
- Transitions to TutorialState on game start trigger

**TutorialState**:
- Handles tutorial scene
- On completion: calls `StartFormalCampaign()` to generate Node 1 options
- Shows double doors for route selection
- Transitions to GameplayState after route selected

**GameplayState**:
- All combat encounters (MissionChoice, Elite, FinalBoss)
- No parameters - reads current node from `CampaignData.CurrentNodeNumber`
- Loads scene from `CampaignData.ActiveRoom.sceneName`
- Subscribes to `BattleEventManager` events
- Transitions to StageClearState on objective resolved

**StageClearState**:
- Handles post-combat flow
- Calls `Campaign.ResolveCurrentEncounter(outcome)` to record result
- After reward collected: calls `Campaign.TryPrepareNextStep()` to determine next action
- Shows doors (single or double) or transitions to GameEndState if FinalBoss defeated

**GameEndState**:
- End-of-run summary
- Transitions back to HQState

---

## 4. Mission Lifecycle

### Encounter Phases

```
Loading
  ↓
Briefing
  ↓
Active
  ↓
ObjectiveResolved
  ↓
RewardSelection
  ↓
RouteSelection
  ↓
Transitioning
```

**Phase Management**:
- Tracked by `BattleEventManager.CurrentEncounterPhase`
- Each phase can only be entered once
- Phase transitions triggered by events

**Mission Types**:
- `EliminateAll` - No timer, complete when all enemies neutralized
- `ReachVotePercent` - Timed, complete when vote percentage reached
- `Survive` - Timed, complete when time expires

---

## 5. System Ownership Map

| System | Owner | Persistent Data Location | Primary Events | Primary Files |
|--------|-------|--------------------------|----------------|---------------|
| Campaign | GameDB.Campaign | CampaignData | - | CampaignDefinition.cs, CampaignData struct |
| Mission | MissionTracker | CampaignData.ActiveRoom | OnObjectiveResolved | MissionTracker.cs, RoomMissionData.cs |
| GameFlow | GameFlowManager | stateMachine | - | GameFlowManager.cs, States/*.cs |
| Battle Events | BattleEventManager (static) | None | OnObjectiveResolved, OnRewardCollected, OnRouteSelected, OnFinalBossDefeated | BattleEventManager.cs |
| Resources | GameDB.Run | RunData | OnIntegrityChanged, OnFundsChanged, OnVotesChanged | GameDB.cs |
| Doors/Exits | RoomExitController | None | OnRouteSelected, OnExitReached | RoomExitController.cs, DoorController.cs |
| Audio | AudioManager | DontDestroyOnLoad | - | AudioManager.cs |
| Rewards | RewardItemSpawner | None | OnRewardCollected | RewardItemSpawner.cs, RewardItem.cs |
| UI | UIManager | DontDestroyOnLoad | - | UIManager.cs |

---

## 6. Relevant Files by Subsystem

### Campaign / GameFlow

**Primary**:
- `Assets/Scripts/System/GameDB.cs` - Single source of truth for all game state
- `Assets/Scripts/System/Mission/CampaignDefinition.cs` - Campaign structure definition
- `Assets/Scripts/GameFlow/GameFlowManager.cs` - Global state machine
- `Assets/Scripts/GameFlow/States/TutorialState.cs` - Tutorial state
- `Assets/Scripts/GameFlow/States/GameplayState.cs` - Combat state
- `Assets/Scripts/GameFlow/States/StageClearState.cs` - Post-combat state
- `Assets/Scripts/GameFlow/States/HQState.cs` - Headquarters state
- `Assets/Scripts/GameFlow/States/GameEndState.cs` - End-of-run state
- `Assets/Data/Mission/CampaignDefinition.asset` - Campaign configuration

**Read if needed**:
- `Assets/Scripts/GameFlow/States/BootState.cs`
- `Assets/Scripts/GameFlow/States/MainMenuState.cs`

**Do not edit for unrelated tasks**:
- State machine infrastructure
- Campaign node role logic

### Mission / Route

**Primary**:
- `Assets/Scripts/System/Mission/MissionTracker.cs` - Mission objective tracking
- `Assets/Scripts/System/Mission/RoomMissionData.cs` - Mission definitions (ScriptableObject)
- `Assets/Scripts/System/Mission/MissionPool.cs` - Mission selection pool
- `Assets/Scripts/System/RoomExitController.cs` - Door management
- `Assets/Scripts/System/Mission/DoorController.cs` - Individual door control
- `Assets/Scripts/GameFlow/BattleEventManager.cs` - Battle event dispatcher

**Read if needed**:
- `Assets/Scripts/System/Mission/DoorPreviewZone.cs` - Door proximity detection

**Do not edit for unrelated tasks**:
- Mission objective type enums
- Encounter phase logic

### Enemy / Combat

**Primary**:
- `Assets/Scripts/Enemy/EnemyController.cs` - Enemy AI controller
- `Assets/Scripts/Enemy/EnemyHealth.cs` - Enemy health management
- `Assets/Scripts/Enemy/States/` - Enemy state machine states

**Read if needed**:
- `Assets/Scripts/System/BattleFlowController.cs` - Battle flow coordination

**Do not edit for unrelated tasks**:
- Enemy state machine infrastructure

### Player / Skills

**Primary**:
- `Assets/Scripts/Player/PlayerController.cs` - Player control
- `Assets/Scripts/Player/PlayerAttack.cs` - Player attack logic
- `Assets/Scripts/Player/PlayerSkillManager.cs` - Skill management
- `Assets/Scripts/Player/StateMachine/` - Player state machine states

**Read if needed**:
- `Assets/Scripts/Player/PlayerStats.cs`

**Do not edit for unrelated tasks**:
- Player state machine infrastructure

### Policy / Build

**Primary**:
- `Assets/Scripts/PolicyCard/PolicyCardData.cs` - Policy card definitions (ScriptableObject)
- `Assets/Scripts/PolicyCard/ICardEffect.cs` - Card effect interface
- `Assets/Scripts/PolicyCard/Effects/` - Card effect implementations
- `Assets/Scripts/PolicyCard/PolicyEffectRuntimeManager.cs` - Runtime effect management

**Read if needed**:
- `Assets/Scripts/FactionData.cs` - Faction definitions

**Do not edit for unrelated tasks**:
- Card effect interface

### Voter / Social Atmosphere

**Primary**:
- `Assets/Scripts/Voter/VoterLogic.cs` - Voter AI logic
- `Assets/Scripts/Voter/VoterData.cs` - Voter data component
- `Assets/Scripts/Voter/States/` - Voter state machine states
- `Assets/Scripts/System/VoteManager.cs` - Vote counting

**Read if needed**:
- `Assets/Scripts/Voter/VoterConfig.cs` - Voter configuration (ScriptableObject)

**Do not edit for unrelated tasks**:
- Voter state machine infrastructure

### Audio

**Primary**:
- `Assets/Scripts/Audio/AudioManager.cs` - Audio system manager
- `Assets/Scripts/Audio/AudioTester.cs` - Audio testing utility

**Read if needed**:
- State files that call AudioManager (MainMenuState, HQState, GameplayState, TutorialState)

**Do not edit for unrelated tasks**:
- AudioManager singleton pattern

---

## 7. Critical Invariants

### Data Flow

- **Cross-scene runtime state** → GameDB (never in Managers)
- **Campaign progress** → CampaignData (CurrentNodeNumber, Results)
- **UI numeric display** → event subscription (never Update polling)
- **Stage transition** → GameFlow state changes
- **Mission completion** → BattleEventManager events

### Architecture Rules

- **FinalBoss identification**: Use `isFinalBossOpponent` flag, not room number magic numbers
- **Node role determination**: Use `CampaignDefinition.GetRoleForNode()`, not hardcoded node numbers
- **Scene loading**: Always async via Coroutine in State.Enter()
- **Event subscription**: Subscribe in Coroutine after scene loaded, unsubscribe in State.Exit()

### Deleted Obsolete Architecture

The following components have been removed and must NOT be reintroduced:
- `RouteDoorSpawner` - replaced by RoomExitController
- `MapNodeButton` - removed with Block system
- `HQExitTrigger` - replaced by StartGame.cs
- Block system (Block 1/2/3) - replaced by 12-node campaign
- Safe room system - removed entirely
- `GameplayState(int roomNumber)` constructor - replaced by parameterless constructor

---

## 8. Known Technical Debt

### MissionTracker Timing

**Issue**: `ReachVotePercent` missions require `_subscribed` flag to prevent premature completion
**Location**: `MissionTracker.cs`
**Workaround**: Added `_subscribed` boolean and check in Update()
**Future**: Any new polling-based mission types must include this check

### Elite Scene

**Issue**: Node 6 (Elite) currently uses `TestMVP` scene
**Reason**: Original `TestSmallBoss` scene lacks MissionTracker, RewardItemSpawner, BattleFlowController
**Future**: When creating dedicated Elite scene, must include all required components

### Tutorial Event Compatibility

**Issue**: Tutorial still uses `OnRoomCleared` for compatibility
**Location**: `TutorialState.cs`, `TutorialManager.cs`
**Future**: Migrate to new event flow (OnObjectiveResolved, etc.) when tutorial redesigned

---

## 9. Verification Status

### Verified Against Code

- ✅ GameDB.cs exists at `Assets/Scripts/System/GameDB.cs`
- ✅ CampaignDefinition.cs exists with 12-node constant
- ✅ State files exist in `Assets/Scripts/GameFlow/States/`
- ✅ MissionTracker.cs exists with timing fix
- ✅ BattleEventManager.cs exists as static class
- ✅ Obsolete files (RouteDoorSpawner, MapNodeButton, HQExitTrigger) deleted
- ✅ GameplayState uses parameterless constructor
- ✅ 12-node architecture implemented

### Design Decision Only

**Current Design**: See `CURRENT_DESIGN_DECISIONS.md` for:
- Elite/Boss victory conditions
- Enemy neutralization (not death)
- Skill progression system
- Political method framework

### Needs Future Implementation

- Narrative system (NarrativeBeat, NarrativeDirector)
- Seed selection UI
- Distress reward balancing
- Complete enemy AI behaviors
- Ending cinematics

---

## 相關文檔 (Related Documents)

- **.agents/AGENTS.md** - 「AI 寫程式必須遵守什麼？」
- **.agents/CURRENT_DESIGN_DECISIONS.md** - 「現在遊戲設計決策是什麼？」
- **docs/systems/AUDIO_SYSTEM.md** - 「音效系統怎麼使用？」
- **docs/archive/** - 「以前做過什麼？」(歷史紀錄，非規範)

---

**Last Updated**: 2026-10-07  
**Status**: Canonical Architecture Reference
