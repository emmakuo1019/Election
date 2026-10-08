# AUDIO_SYSTEM.md

**Last Updated**: 2026-10-07  
**Status**: Subsystem Documentation

---

## Overview

Audio system managing background music (BGM) and sound effects (SFX) across all scenes. Uses a singleton pattern with DontDestroyOnLoad to persist across scene transitions.

---

## Core Components

### AudioManager
- **Location**: `Assets/Scripts/Audio/AudioManager.cs`
- **Pattern**: Singleton, DontDestroyOnLoad
- **Responsibility**: BGM/SFX playback, volume control, automatic scene music management

### AudioTester
- **Location**: `Assets/Scripts/Audio/AudioTester.cs`
- **Purpose**: Testing utility for audio playback verification

---

## Audio Assets

### Required Files

**Background Music (2 files)**:
- `HQ_BGM` - Main menu and headquarters
- `Mission_BGM` - All combat encounters (Tutorial, MissionChoice, Elite, FinalBoss)

**Sound Effects (5+ files)**:
- `Attack_Normal` - Normal attack sound
- `Convert_Success` - Successful voter conversion
- `Enemy_Volunteer_Death` - Enemy volunteer neutralized
- `Enemy_Convert_Success` - Enemy successful conversion
- `Skill_Dogeza` - Dogeza skill

### Asset Locations

```
Assets/Audio/
├── BGM/              # Background music
├── SFX/              # Sound effects
├── README.md         # Technical documentation
└── 組員使用手冊.md   # Team member guide
```

---

## Audio State Mapping

### Current Implementation

| Game State | BGM | Notes |
|------------|-----|-------|
| MainMenuState | HQ_BGM | Shared with HQ |
| HQState | HQ_BGM | Same music continues if from MainMenu |
| TutorialState | Mission_BGM | Switches from HQ_BGM |
| GameplayState | Mission_BGM | All combat (MissionChoice, Elite, FinalBoss) |
| StageClearState | Mission_BGM | Continues during reward selection |
| GameEndState | (TBD) | End-of-run state |

**Note**: Campaign progression details are defined in `/KIRO_PROJECT_CONTEXT.md` and `/CURRENT_DESIGN_DECISIONS.md`. This document only describes audio system behavior.

### Automatic Transitions

```
Main Menu (HQ_BGM)
    ↓
HQ (HQ_BGM) - no restart, same music
    ↓
Tutorial (Mission_BGM) - fade transition 1.5s
    ↓
Combat (Mission_BGM) - no restart, same music
    ↓
HQ (HQ_BGM) - fade transition 1.5s
```

---

## System Features

### BGM Management

- **Automatic scene switching**: Music changes automatically based on GameFlow state
- **Fade transitions**: 1.5 second crossfade when switching BGM
- **Smart continuation**: Same BGM doesn't restart when moving between scenes
- **Single AudioSource**: One dedicated BGM AudioSource with loop enabled

### SFX Management

- **Audio source pool**: 8 SFX AudioSources for simultaneous playback
- **Non-interrupting**: Multiple sound effects can play at once
- **Automatic cycling**: Pool automatically reuses available sources

### Volume Control

- **Independent control**: Separate BGM and SFX volume (0.0 - 1.0)
- **Master volume**: Global AudioListener.volume
- **Mute support**: Pause/Resume and Stop functionality

---

## Inspector Setup

### AudioManager Configuration

1. Create AudioManager GameObject in any scene (typically MainMenu)
2. Add AudioManager component
3. Drag audio clips into Inspector fields:

**BGM Section**:
- `bgmMenuAndHQ` → HQ_BGM file
- `bgmMission` → Mission_BGM file

**SFX Section**:
- `sfxAttackNormal` → Attack_Normal.wav
- `sfxConvertSuccess` → Convert_Success.wav
- `sfxEnemyVolunteerDeath` → Enemy_Volunteer_Death.wav (or Neutralized)
- `sfxEnemyConvertSuccess` → Enemy_Convert_Success.wav
- `sfxSkillDogeza` → Skill_Dogeza.wav

**Volume Section**:
- `bgmVolume` - Default: 0.7
- `sfxVolume` - Default: 1.0

AudioManager automatically creates required AudioSource components.

---

## API Reference

### BGM Control

```csharp
// Play background music (with fade transition)
AudioManager.Instance.PlayBGM_MenuAndHQ();
AudioManager.Instance.PlayBGM_Mission();

// Stop with fade out
AudioManager.Instance.StopBGM();

// Pause/Resume
AudioManager.Instance.PauseBGM();
AudioManager.Instance.ResumeBGM();

// Volume control
AudioManager.Instance.SetBGMVolume(float volume); // 0.0 - 1.0
```

### SFX Playback

```csharp
// Play sound effects
AudioManager.Instance.PlaySFX_AttackNormal();
AudioManager.Instance.PlaySFX_ConvertSuccess();
AudioManager.Instance.PlaySFX_EnemyVolunteerDeath();
AudioManager.Instance.PlaySFX_EnemyConvertSuccess();
AudioManager.Instance.PlaySFX_SkillDogeza();

// Volume control
AudioManager.Instance.SetSFXVolume(float volume); // 0.0 - 1.0
```

---

## State Integration

### Automatic BGM Calls

The following GameFlow states automatically manage BGM:

**MainMenuState.Enter()**:
```csharp
AudioManager.Instance?.PlayBGM_MenuAndHQ();
```

**HQState.Enter()**:
```csharp
AudioManager.Instance?.PlayBGM_MenuAndHQ();
```

**TutorialState** (in scene load coroutine):
```csharp
AudioManager.Instance?.PlayBGM_Mission();
```

**GameplayState** (in scene load coroutine):
```csharp
AudioManager.Instance?.PlayBGM_Mission();
```

**Note**: SFX calls are triggered by gameplay events (e.g., PlayerAttack, VoterLogic, EnemyController).

---

## Technical Implementation

### Architecture

```
AudioManager (Singleton, DontDestroyOnLoad)
├── BGM AudioSource (1 instance)
│   ├── Loop: true
│   ├── Volume: bgmVolume
│   └── Fade support
│
├── SFX AudioSource Pool (8 instances)
│   ├── Loop: false
│   ├── Volume: sfxVolume
│   └── Cyclic reuse
│
└── Clip References (Inspector assigned)
    ├── bgmMenuAndHQ
    ├── bgmMission
    ├── sfxAttackNormal
    ├── sfxConvertSuccess
    ├── sfxEnemyVolunteerDeath
    ├── sfxEnemyConvertSuccess
    └── sfxSkillDogeza
```

### Fade Implementation

BGM transitions use coroutine-based crossfade:
1. New clip starts at volume 0
2. Old clip fades out over 1.5s
3. New clip fades in simultaneously
4. Old clip stops when volume reaches 0

---

## TODO / Future Work

### Naming Migration

**Current**: `EnemyVolunteerDeath` in API and asset names

**Future Target**: `EnemyVolunteerStunned` or `EnemyVolunteerNeutralized`

This aligns with current design decision that enemies are **neutralized** (temporarily disabled) rather than permanently killed.

**Action Required**: 
- Update API method names in AudioManager.cs
- Rename audio assets
- Update all call sites

**This documentation update only - no code changes in this task.**

---

## Testing

### Using AudioTester

1. Add AudioTester component to any GameObject
2. **Inspector Mode**: Check test buttons in Inspector to trigger sounds
3. **Runtime Mode**: Press Play, use on-screen GUI buttons (F3 key toggle if implemented)

### Manual Testing Checklist

- [ ] Main Menu plays HQ_BGM
- [ ] HQ continues HQ_BGM (no restart)
- [ ] Tutorial switches to Mission_BGM with fade
- [ ] Combat continues Mission_BGM (no restart)
- [ ] Returning to HQ switches back to HQ_BGM with fade
- [ ] SFX plays correctly during gameplay
- [ ] Volume sliders work for both BGM and SFX
- [ ] Multiple SFX can play simultaneously

---

## Troubleshooting

### Music doesn't play
- Check AudioManager exists in scene (created once, persists)
- Verify audio clips assigned in Inspector
- Check AudioListener exists in scene

### Music restarts when it shouldn't
- Verify state is calling correct Play method
- Check if same BGM is already playing (should not restart)

### SFX cuts off or doesn't play
- Check SFX pool size (default 8 sources)
- Verify SFX clips assigned in Inspector
- Check SFX volume not set to 0

---

## Related Documentation

- **.agents/KIRO_PROJECT_CONTEXT.md** - GameFlow state architecture
- **.agents/CURRENT_DESIGN_DECISIONS.md** - Game design decisions
- **Assets/Audio/README.md** - Detailed technical guide (if exists)
- **Assets/Audio/組員使用手冊.md** - Team member guide (if exists)

---

**Last Updated**: 2026-10-07  
**Status**: Subsystem Documentation
