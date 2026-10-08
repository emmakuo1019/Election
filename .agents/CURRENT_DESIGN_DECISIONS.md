# CURRENT_DESIGN_DECISIONS.md

**Last Updated**: 2026-10-07  
**Status**: Canonical Game Design Reference  
**Authority**: User-confirmed design decisions

---

## Before Reading This Document

This document describes **what the game should be designed as**.

For **how the code actually works**, see `.agents/KIRO_PROJECT_CONTEXT.md`.  
For **permanent coding rules**, see `.agents/AGENTS.md`.

---

## 1. Core Experience

### 核心理念

將政治博弈動作化。

**Faction 是 Build**

每個 Faction 都包含：
- **Governance / Policy-oriented** (治理取向)
- **Mobilization / Populist-oriented** (動員取向)

玩家不是在選：**好派 vs 壞派**

而是在同一政治路線下，持續選擇不同政治手段。

### 道德模糊性

任何 Faction 都不得天然等於：
- ❌ 理性 = 善
- ❌ 情緒 = 惡
- ❌ 正確 / 錯誤

玩家的道德取向由「局內持續政策選擇」形成，而不是「選擇 Faction 時就先決定善惡」。

---

## 2. Campaign Structure

### 正式流程

**固定 12 個正式節點**

Tutorial 不算正式節點。

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

**簡化表示**：
```
Mission × 5 → Elite → Mission × 5 → FinalBoss
```

### 已廢止的舊設計

以下設計已正式廢止，**不得**出現在 Current Design：

- ❌ 固定 8 節點
- ❌ Elite = Node 4
- ❌ FinalBoss = Node 8
- ❌ Opening 作為 Node 1 的固定舊流程

---

## 3. Mission Design

### Mission 才是「關卡內容」

Scene 不是關卡本身。

Mission 應定義：
- Objective (目標)
- Scene (場景)
- Enemy composition (敵人組成)
- Mission parameters (任務參數)
- Reward context (獎勵情境)

### 核心 Mission 類型

目前核心類型：
- **EliminateAll** - 消滅對手
- **ReachVotePercent** - 達到 X% 票倉
- **Survive** - 生存 N 秒

### EliminateAll 語意

**重要**：EliminateAll 類型主要對應：
- 記者會
- 記者干擾

等情境。

一般搶票關卡中的主要敵對干擾者則多為：
- Opponent Volunteer (對手志工)

### 程式命名說明

**現狀**：
- EliminateAll 名稱目前沿用既有程式命名
- 未來實際完成條件應對應 **Neutralize / 暫時排除**
- 而非永久死亡

**標記**：
```
Current Implementation: EliminateAll (程式 Enum)
Current Design Decision: Neutralize all threats temporarily
```

本次文件整理**不得修改程式 Enum**。

---

## 4. Build / Faction

### Faction = Build Identity

**Faction** 才是 Build Identity。

不要把以下當成兩條 Build：
- ❌ Rational
- ❌ Emotion

### 不同 Faction 是

- 不同政治派系
- 不同政策組合
- 不同 Skill Upgrade Path
- 不同玩法傾向

### 重要原則

任何 Faction 都不得天然等於：
- ❌ 理性
- ❌ 情緒
- ❌ 善
- ❌ 惡
- ❌ 正確
- ❌ 錯誤

---

## 5. Political Method

### 設計語意

使用以下框架描述政治手段：

**Policy / Governance-oriented** (政策 / 治理取向)：
- 有規劃的政見
- 治理手段
- 較完整、有成本、有規劃的政策

**Mobilization / Populist-oriented** (動員 / 民粹取向)：
- 群眾動員
- 民粹式操弄
- 更有效率，但利用焦慮、認同或群眾情緒

### 不得使用的簡化

不得使用以下簡化框架：
- ❌ Rational = good
- ❌ Emotion = bad

### 設計目的

玩家即使選擇同一 Faction，仍然可能透過不同政策卡，形成完全不同的政治人物。

**例如**：

同一 Faction 可能同時有：

**A. 較完整、有成本、有規劃的政策**

**B. 更有效率，但利用焦慮、認同或群眾情緒的動員方式**

---

## 6. Skill Progression

### 完整流程

技能成長**不再使用**固定關卡解鎖。

也**不再單純**「達門檻就直接解鎖技能」。

**正式流程**：

```
取得某個 Faction 的 Policy Card
    ↓
累積同一 Faction 的卡片數量
    ↓
達到指定門檻
    ↓
該 Faction 對應的 Skill Upgrade Card
開始具備進入 Reward Pool 的資格
    ↓
玩家實際抽到該卡
    ↓
玩家選擇該卡
    ↓
對應技能直接升級
```

### 重要區別

**「達門檻」只代表**：

```
Skill Upgrade Card Eligible
```

**不是直接升級**。

Skill Upgrade Card 必須：
- 被抽到
- **+**
- 被玩家選擇

之後才升級。

### 門檻數字

目前具體門檻數字：**TBD / Balance Parameter**

**不得**在 Canonical Design 中宣稱固定為：
- ❌ 3
- ❌ 6
- ❌ 或其他尚未確認數值

### 程式設計要求

未來程式設計時：

門檻應**資料化 / configurable**。

**不得**散落 hard-code 在 Gameplay Logic 中。

### 已廢止的舊設計

以下舊設計已正式廢止：

- ❌ Fixed stage skill unlock
- ❌ Direct threshold skill unlock (達門檻直接解鎖)

---

## 7. Enemy Defeat Rules

### 一般敵人

一般敵人未來**不應使用**真正死亡作為主要結果。

**設計規則**：

```
HP / 可受擊資源歸零
≠ Death

而是：

HP 歸零
→ Neutralized / Knocked Down / Stunned
→ 暫時失去行動能力
→ 經過設定時間後恢復
```

### Current Implementation vs Current Design

目前若程式仍存在 Death / Die() 等舊實作，文件應清楚區分：

**Current Implementation**: 可能仍有 Death 相關程式碼

**Current Design Decision**: Neutralize (暫時失能)，而非永久死亡

本次**不得修改程式**。

---

## 8. Elite / FinalBoss

### 核心規則

**Elite 與 FinalBoss 都不可被擊殺**。

**設計規則**：

- ❌ 不以 Boss HP 歸零判定勝利
- ❌ 玩家攻擊不應降低 Boss HP
- ✅ Boss 可以受到 Stun
- ✅ Boss Stun 結束後恢復行動
- ✅ 勝利條件使用 **Vote Objective / 得票目標**

### 已廢止的舊規則

以下舊規則已廢止：

```
❌ Boss HP = 0 → Victory
```

### Elite / FinalBoss 技能

目前 Elite / FinalBoss **只保留兩種主要特殊技能**：

#### 1. Summon (召喚)
- 召喚小怪 / 干擾單位

#### 2. Mass Persuasion (群眾拉票)
- 大範圍拉票
- 改變附近選民支持傾向

### FinalBoss 難度差異

FinalBoss 可以透過以下參數與 Elite 做難度差異：

- 更短 Cooldown
- 更高影響量
- 更多召喚數量
- 更高壓力參數

**不要透過新增大量新系統做差異**。

### 已廢止的第三個技能

以下舊設計**正式廢止**：

- ❌ EnemySkill_DisruptPlayer
- ❌ Reset Player Skill Cooldown
- ❌ BossDisrupt.asset
- ❌ FinalBoss 第三個「冷卻干擾」技能

這些只能存在於 **Historical Context** (`docs/archive/`)。

**不得**出現在 Current Design。

---

## 9. Reporter Interaction

### Reporter / 記者

Reporter / 記者是特殊敵人。

**目前設計決策**：

- ✅ 可以受到玩家攻擊
- ✅ HP 歸零後進入暫時失能 / Stun
- ❌ 不是永久死亡
- ✅ 恢復後可繼續活動
- ✅ 具有監視範圍

### 記者監視處罰規則

**不得**再寫成：

```
❌ 「所有情緒技能一律受到處罰」
```

**正確設計是**：

```
✅ 「特定技能本身具有是否會在記者監視下受到處罰的屬性 / 標記。」
```

也就是：

**Reporter 判斷技能是否屬於可被媒體抓包的手段**

而不是：

單純判斷 Emotional / Rational

### Reporter 對 Elite / Boss 的效果

若 Reporter 的攻擊或干擾效果能作用於 Elite / Boss：

✅ Elite / Boss 仍可被 Stun

---

## 10. Social Atmosphere

### Social Atmosphere 不是 Build

**區別**：

- **Faction** = 玩家正在建構哪一個玩法 Build
- **Political Method** = 玩家使用什麼政治手段
- **Social Atmosphere** = 這些選擇長期累積後，世界與群眾變成什麼樣子

### Social Atmosphere 是什麼

Social Atmosphere 是：
- 世界狀態 (world state)
- 系統後果 (system consequence)
- 敘事後果 (narrative consequence)

### Social Atmosphere 可以影響

它可以繼續影響：

- 選民生成
- 支持穩定度
- 世界視覺
- 敘事結果
- Ending interpretation

### Social Atmosphere 不得取代

**但不得取代 Faction Build**。

---

## 11. Explicitly Deprecated Design Decisions

### 已廢止設計清單

以下設計已正式廢止，**不得復活**：

#### Campaign Structure
- ❌ 8-node Campaign
- ❌ Elite Node 4
- ❌ Boss Node 8

#### Victory Conditions
- ❌ Boss HP victory
- ❌ Boss HP = 0 → Victory

#### Build Identity
- ❌ Rational Build (作為 Build Identity)
- ❌ Emotion Build (作為 Build Identity)

#### Skill Progression
- ❌ Fixed stage skill unlock
- ❌ Direct threshold skill unlock (達門檻直接解鎖)

#### Boss Skills
- ❌ Boss cooldown disruption skill
- ❌ EnemySkill_DisruptPlayer
- ❌ Reset Player Skill Cooldown
- ❌ BossDisrupt.asset
- ❌ FinalBoss 第三個技能

#### Enemy Mechanics
- ❌ Enemy permanent death (作為主要設計)

#### Architecture
- ❌ Safe Room random path
- ❌ RouteDoorSpawner-based current architecture

---

## 讓 AI 知道哪些舊規則不得復活

這一章非常重要：**讓 AI 知道哪些舊規則不得復活**。

如果未來 AI 在舊文件或註解中看到上述已廢止設計，應：

1. 識別為 Historical / Deprecated
2. 不實作到 Current Design
3. 必要時詢問使用者確認

---

## 參考文件

- **.agents/KIRO_PROJECT_CONTEXT.md** - 「現在程式架構實際怎麼運作？」
- **.agents/AGENTS.md** - 「AI 寫程式必須遵守什麼？」
- **docs/systems/** - 「特定 subsystem 怎麼使用？」
- **docs/archive/** - 「以前做過什麼？」(歷史紀錄，非規範)

---

**Last Updated**: 2026-10-07  
**Status**: Canonical Game Design Reference  
**Authority**: User-confirmed design decisions
