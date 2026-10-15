# 敵人、記者與精英／Boss：分階段實作計畫

日期：2026-10-06
目標分支：`6000.3.17f1_全新的生命`
建檔基準：`b710d40a37ea98d68bc63c6139ade5a2c9e1eb3d`
狀態：計畫已獲使用者授權建立；功能尚未實作、Unity 尚未驗收。

## 1. 使用方式與範圍

這是交給本地 AI 的執行規格。每次只執行一個編號任務，完成、驗證、回報後停止，不自動做下一步。步驟過大時可以拆成子步驟，但不可擴充本輪功能。

開始前讀取適用的 AGENTS.md、本文「定案規則」、進度表及本步指定檔案。先檢查 git status 與目前分支，不覆寫使用者未提交的工作，不重設或清理工作目錄。以最新本地程式為準，用 rg 尋找相關呼叫者；不要每一步重新掃描整個專案或重寫總計畫。

本次保留 EnemyController、IState、NavMeshAgent、EnemySkillData 與現有事件流程。不導入行為樹、評分式 AI、新外掛或大型通用戰鬥框架。既有技能／效果先查找可重用的 ICardEffect／Strategy 積木；新增效果沿用專案規範，不趁此全面改造舊技能。

跨場景資料沿用 GameDB；單隻敵人的耐久、目標與冷卻留在該實例。UI 訂閱事件。Skill ScriptableObject 儲存設定，不存每隻敵人的剩餘冷卻。相關過時文件中的「Boss HP 歸零獲勝」已被本次使用者明確指定的搶票規則取代。

## 2. 定案規則

| 角色 | 行為 | 受擊／退場 | 任務關係 |
|---|---|---|---|
| 志工 | 搶票、既有暈眩技能 | 不可擊殺；耐久歸零暈眩，恢復時補滿耐久 | 主要出現在搶票關，不計入清場目標 |
| 記者 | 跟隨玩家或對手候選人，監視指定技能 | 普攻與 Inspector 指定技能可扣血；其他技能無效；HP 歸零退場 | 記者會的指定記者才計入清場 |
| 精英／Boss | 搶票、對玩家扣血或暈眩技能 | 不扣血但可暈眩；不以死亡事件結束戰鬥 | 達到任務指定票數門檻獲勝 |

記者不是持續接觸傷害敵人：只有被監視候選人在監視範圍內實際施放指定技能時才處罰。玩家受罰扣誠信 HP，精英／Boss 受罰暈眩。志工不是候選人，不列入這一版監視對象。記者不拉票。

「可傷害記者的技能」與「被記者監視的技能」是兩份不同設定，不能混用。

收斂版先做精英一招、Boss 兩招。延期：召喚、玩家技能冷卻重置、隨機技能組合、Boss 多階段。保留既有相關程式資產，不做無關刪除，但正式配置不啟用延期技能。

### 可調整的初始實作預設（不是平衡結論）

- 志工長暈眩期間不再扣耐久或延長暈眩；恢復後才接受新的耐久傷害。
- 一般命中的短硬直與耐久歸零的長暈眩分開，短硬直可設為 0。
- 精英／Boss 重複暈眩不相加、不重設正在進行的暈眩；本輪不做韌性條。
- 同一候選人的同一次施法，最多受到一次記者處罰；多名記者不可疊加。
- 監視採平面距離判定，不新增視線遮蔽系統；限制需寫 ponytail 註解。
- 記者以最近的有效候選人為跟隨目標，保留目前目標直到失效／超出追蹤距離，避免來回切換。
- 精英先配近距離暈眩；Boss 配近距離暈眩與具前搖的扣血招式。重用既有拉票普攻，不增加第三招。
- 沿用有效任務資料的票數目標（先前看到 70%），保持 Inspector 可調；不是硬編碼所有 Boss 都 70%。
- 最終戰未達票數門檻且時限到：進敗選流程；玩家 HP 歸零走既有退選流程。精英沿用普通任務失勢流程。相同幀的終局事件必須只有一個結果，玩家 HP 歸零優先；若需要改設計，先回報差異。
- 具體傷害、耐久、秒數由可調欄位提供，實測再定稿。

## 3. 已知檢查依據與限制

先前靜態檢查基準 ef09c64：
- EnemyController.Update 技能排程未排除暈眩。
- MissionTracker 對 FinalBoss 等待擊殺事件，但 M_Boss 是搶票任務，Boss Prefab 是無敵。
- 三種敵人 Prefab attackRange=2、attackHitRadius=1、offset=0，有停下後打不到目標的風險。
- PlayerAttack 對敵人呼叫 TakeDamage(10, 1.0f)，志工 Prefab maxHP=3；敵人普攻對玩家傷害寫死 1f。
- 精英／Boss Prefab 引用的六個技能 GUID，在該版本 62 個 .asset.meta 中無對應。可能為本地資產未提交，不能據此判定本地也不存在。
- 已有召喚、範圍拉票、冷卻干擾腳本，不代表正式 Prefab 可執行。
- Idle/Wander 有每幀範圍搜尋；PlayerAttack 有額外 Debug 掃描與大量日誌。
- 未找到獨立記者實作。

本次開 PR 前確認分支已更新至 b710d40，新增 AudioManager，修改 Boss_Arena 場景、NavMesh 及部分流程狀態。以上舊腳本問題不是 Unity 執行驗證；實作前需確認最新設定，尤其不要覆寫新的音效與場景修改。沒有效能量測，不保證 FPS 或總 AI 用量。

## 4. 三階段九個任務

共同完成條件：只改本步必要檔案；保留序列化欄位資料及 .meta GUID；非平凡邏輯依 AGENTS.md 留一項最小可執行檢查。沒有 Unity 時記為「待 Unity 驗收」，不可宣稱編譯或 Play Mode 已通過。每步附 Inspector 設定清單與 commit，更新第 6 節後停止。前置未驗證時，不自動繼續相依步驟。

### 階段一：基礎規則

#### 01｜志工耐久歸零改為暈眩循環

依賴：無。
必讀：EnemyController.cs、EnemyStunState.cs、StateMachine.cs、EnemySpawnTracker.cs、Enemy.prefab；搜尋 TakeDamage、Die 呼叫者。
實作：
1. 以最少角色／受擊設定區分志工、記者、精英／Boss；角色欄位先供必要分流，不新增角色繼承樹。
2. 志工耐久歸零不得 Destroy、不得觸發死亡或清場計數；停止移動及原行為，進入長暈眩。
3. 恢復時補滿耐久，回 Idle 重新選目標。停用／重啟不得留下特效或失效目標。
4. 耐久、長暈眩與短硬直獨立可調；UI 對志工使用耐久語意。
Inspector：最大耐久、破防暈眩秒數、一般命中硬直秒數、角色類型。
驗收：耐久歸零兩輪皆存活並恢復；暈眩中再命中不延長；死亡計數不因志工破防變動。
停止點：本步只驗收循環；技能插入暈眩的已知問題留 02，不能因此宣告整個志工完成。

#### 02｜暈眩、攻擊與技能的優先權

依賴：01。
必讀：EnemyController.cs、EnemySkillState.cs、EnemyAttackState.cs、EnemyStunState.cs、EnemySkillData.cs。
實作：
1. 非 Active、暈眩、退場時不得發起攻擊或技能。不要讓冷卻完成強制中止暈眩。
2. 前搖被中斷要取消本次尚未執行的效果與提示；已命中的效果不倒退。
3. 各技能冷卻獨立推進；一次只施放一招。初版冷卻從進入施法時計算，中斷不退還；計時只在 Active 推進。
4. 正在施法或普攻時不插入另一招；後續用統一的可行動檢查，避免多處互相搶狀態。
Inspector：冷卻、首次施放延遲、前搖、總時長；驗證總時長不得短於前搖。
驗收：冷卻在暈眩中歸零仍不出招；前搖最後一刻被打斷不造成延遲傷害；任務結束後不出招。
停止點：不加入新技能或複雜優先權系統。

#### 03｜命中範圍、數值與去重

依賴：02。
必讀：PlayerAttack.cs、EnemyController.cs、EnemyMoveState.cs、EnemyAttackState.cs、AttackRangeMesh.cs；搜尋現有技能的範圍命中入口。
實作：
1. 統一停止距離與真實判定範圍，提示形狀與命中一致；檢查偏移、角度、子 Collider。
2. 同一出手對同一角色最多結算一次，玩家普攻、敵人普攻及本輪啟用技能都遵守。
3. 把普攻傷害、暈眩秒數等寫死值變成序列化設定；保留選民拉票量與角色 HP 傷害的差別。
4. 遷移舊 Prefab 欄位時保持引用及既有合法值，不以 Awake 靜默覆寫所有調整。
Inspector：傷害、拉票量、攻擊距離／半徑／角度、前搖與後搖。
驗收：邊界內命中、外部不命中；雙 Collider 不重複扣值；志工依設定可承受多次命中。
停止點：不改政策卡成長公式，不改玩家整套操作。

#### 04｜精英／Boss 資產、導航與搶票結算

依賴：03。
必讀：Elite_.prefab、Boss_ .prefab、M_Elite.asset、M_Boss.asset、S_Elite.asset、S_Boss.asset、CampaignDefinition.asset、MissionTracker.cs、BattleFlowController.cs、GameplayState.cs、StageClearState.cs、EnemySpawner.cs。
實作：
1. 先檢查最新本地技能資產與 GUID；存在則正確提交，不盲目重建。缺少時只建立本輪需要的配置，連同 .meta 提交。
2. 檢查實際任務引用的是哪個 SpawnConfig；不要僅依名稱修改未被引用的 EliteSpawnConfig／FinalBossSpawnConfig。
3. 驗證生成位置、Agent 類型、Layer、NavMesh 路徑；無有效路徑需有可辨識警告，不能假定 Warp 成功。
4. FinalBoss 改由任務搶票結果推進，不再依賴死亡事件；查所有舊事件呼叫者／訂閱者。精英不誤進結局，Boss 不誤開下一關雙門。
5. 達標、超時、HP 歸零皆只結算一次；清場停用戰鬥，維持現有音效與敘事接點。
Inspector：有效技能槽、SpawnConfig、目標票數、時限、場景引用。
驗收：精英正常移動拉票；票數達標完成；Boss 達標勝選、逾時敗選、玩家 HP 歸零退選；結算後無殘留攻擊。
停止點：只驗證現有可用技能；完整一招／兩招配置留 07。

### 階段二：角色成立

#### 05｜記者受擊、退場與清場任務計數

依賴：04。
必讀：PlayerAttack.cs、EnemyController.cs、EnemySpawnTracker.cs、EnemySpawner.cs、MissionTracker.cs、RoomMissionData.cs；搜尋玩家技能傷害入口。
實作：
1. 最小新增記者角色／Prefab，共用必要移動和受擊能力；不要讓記者沿用拉票或對玩家普攻。
2. 受擊入口帶必要的來源種類與技能引用，既有所有傷害呼叫者一併適配，不能因舊入口繞過限制。
3. 記者只接受普攻與指定技能清單；HP 歸零退場且只通知一次。
4. 清場追蹤以「本次任務指定的記者」為集合，與全場敵人存活數分離。任務開始／生成就緒後才能判零，防止尚未生成即過關。
5. 志工、精英／Boss、非任務記者不計入。場景清理／停用不得當作擊敗；實際生成失敗或零目標應報設定錯誤，不自動成功。
Inspector：記者 HP、可傷害技能清單、本任務目標資格／生成配置。
驗收：一般技能無效、普攻有效、白名單技能有效；混有志工時清掉指定記者仍過關；重複受擊／Destroy 不重複計數。
停止點：記者監視與跟隨留 06。

#### 06｜記者跟隨與監視處罰

依賴：05。
必讀：PlayerSkillManager.cs、玩家 SkillState、EnemySkillState.cs、EnemySkillData.cs、BattleEventManager.cs、PlayerHealthSystem.cs，以及 05 的記者程式。
實作：
1. 使用現有事件模式，在技能真正執行時發出一次施法事件，包含施法者、技能引用、單次施法識別；按鍵、冷卻失敗、前搖取消不算。
2. 記者按可調間隔找候選人，追蹤並保持距離；失去目標才重選。監視範圍內的候選人施法皆可觸發，不限跟隨目標。
3. 監視技能清單與可傷害記者清單獨立。玩家處罰走既有 HP 系統，精英／Boss 走暈眩入口，不改其 HP。
4. 以候選人＋單次施法去重處罰，設定處罰冷卻；多名記者不可放大同次處罰。避免無界累積歷史施法 ID。
5. 事件在停用／退場／場景切換解除；只在 Active 且記者有效時監視。提供清楚監視範圍與處罰回饋。
Inspector：跟隨對象、追蹤／保持／監視距離、換目標檢查間隔、監視技能清單、HP 扣值、Boss 暈眩時間、處罰冷卻。
驗收：範圍外、不在清單、施法失敗均不罰；有效施法正確處罰雙方；兩名記者不疊加；記者退場後不再處罰。
停止點：不加入視線遮蔽、記者派系或新聞評分系統。

#### 07｜精英一招、Boss 兩招與施放條件

依賴：06。
必讀：EnemyController.cs、EnemySkillData.cs、EnemySkillState.cs、現有 ICardEffect／技能效果、正式精英與 Boss Prefab。
實作：
1. 精英配置近距離暈眩；Boss 配置暈眩與扣血技能，數值全部可調，效果優先重用。
2. 只在有效目標與施放範圍成立時出招；不合條件不能占用狀態或消耗本次施法。以簡單固定順序與共用出招間隔避免連續無空檔。
3. 範圍、方向與前搖提示共用同一判定資料；預設施法開始鎖定方向，玩家離開範圍可以躲過。
4. 暈眩可打斷前搖；技能執行正確觸發 06 監視事件。
5. 停用正式配置中的召喚與重置冷卻技能，保留原腳本資產。
Inspector：技能引用、出招間隔、施放距離、前搖、半徑／角度、扣血／暈眩數值。
驗收：精英一招、Boss 兩招各可命中、躲避及被中斷；技能後恢復拉票；沒有玩家在施法範圍時不空放。
停止點：不新增戰術評分、變身或第三招。

### 階段三：驗收與必要優化

#### 08｜Inspector 整理與索敵節流

**任務名稱**：Inspector 整理與索敵節流  
**計畫更新日期**：2026-10-15  
**依賴**：Task 07 程式碼完成（靜態部分無需等待 Unity Play Mode 驗收）。

---

**要解決的問題**

Task 01–07 累積了兩類已知品質問題：

1. **Inspector 可讀性不足**：`EnemyController` 的 Header 分組橫跨戰鬥、角色、UI、除錯等四大類，但混排無邏輯順序；距離欄位缺少 `escapeRange > detectionRange` 的 OnValidate 驗證（僅在 Start 以 `Debug.LogWarning` 修正，不阻止錯誤配置）；`EnemySkillData.OnValidate` 只驗證 `duration >= skillWindupTime`，缺少 `initialDelay >= 0`、`cooldown > 0` 等最基本保護；`castRangeMultiplier` 的說明與 `blastRadius` 的關係未在 Inspector 明示。

2. **每幀索敵 OverlapSphere**：`EnemyIdleState.Update()` 在無目標時每幀呼叫 `FindNearestTarget()`，而 `FindNearestTarget()` 內部執行兩次 `Physics.OverlapSphere`。場景中存在多隻 Idle 敵人時，每幀都在做無謂的物理查詢。計畫 Task 08 原文已明確要求「索敵改可調間隔」。

---

**repo 現況證據**

| 項目 | 位置 | 現況 |
|---|---|---|
| 每幀索敵 | `EnemyIdleState.cs` L55–L67 | `if (ctx.target == null) { ctx.FindNearestTarget(); }` 無間隔節流 |
| OnValidate 距離關係 | `EnemyController.cs` Start() L538 | `Debug.LogWarning + runtime fix`，不是編輯期 OnValidate |
| OnValidate 技能時間 | `EnemySkillData.cs` L21–24 | 只驗 `duration >= skillWindupTime`，缺 `cooldown > 0`、`initialDelay >= 0` |
| castRangeMultiplier 說明 | `EnemyController.cs` L133 | Tooltip 寫了公式，但未說明預設值 1.5 的實際效果（blastRadius×1.5） |
| 常駐 Debug.Log（監視處罰過濾） | `ReporterMonitor.cs` L137–L173 | 每次過濾都輸出 Log，無開關，正常遊玩時持續洗版 |
| 技能排程 Debug.Log | `EnemyController.cs` L691, L723 | 已有 `debugSkillScheduling` 開關保護，符合規範，不需修改 |

---

**本次範圍**

包含：
- `EnemyIdleState.cs`：加入 `_searchTimer` 與可調 `searchInterval`（從 `ctx` 讀取）；節流索敵，不改任何狀態切換邏輯。
- `EnemyController.cs`：新增 `searchInterval` 欄位（帶 Header、Tooltip、`[Min(0.1f)]`）；將 Start() 的距離修正搬移至 `OnValidate()`。
- `EnemySkillData.cs`：`OnValidate` 補充 `cooldown > 0`、`initialDelay >= 0` 的防呆 clamp。
- `ReporterMonitor.cs`：將五行常駐 Debug.Log 加上 `[SerializeField] private bool debugMonitor` 開關，預設 `false`。

不包含（明確排除）：
- EnemyWanderState、PlayerAttack、UI 的整理（Task 08 原文提及但非本輪最小必要）
- 自訂 Editor Inspector 面板
- ECS / Jobs / 全新池化系統
- 效能量測與宣稱百分比
- Task 07 Unity Play Mode 尚待驗收的行為邏輯

---

**前置條件與相依項目**

- Task 07 程式碼完成（HEAD `0260c1aa`），`equippedSkills` 欄位結構與 `EnemySkillData` 類型已確定。
- `EnemyIdleState` 目前透過 `ctx.detectionRange` 做 OverlapSphere，新增 `ctx.searchInterval` 欄位不改動任何現有欄位。
- `ReporterMonitor.cs` 的 `debugMonitor` 欄位為新增 SerializeField，不影響既有 Reporter prefab 序列化（預設值 false）。

---

**預計修改檔案及用途**

| 檔案 | 修改目的 |
|---|---|
| `Assets/Scripts/Enemy/EnemyController.cs` | 新增 `searchInterval` 欄位；將 Start() 距離修正搬到 `OnValidate()` |
| `Assets/Scripts/Enemy/EnemyIdleState.cs` | 加入 `_searchTimer` 節流；每隔 `ctx.searchInterval` 才呼叫 `FindNearestTarget()` |
| `Assets/Scripts/Enemy/EnemySkillData.cs` | `OnValidate` 補 `cooldown` 與 `initialDelay` 最小值保護 |
| `Assets/Scripts/Enemy/ReporterMonitor.cs` | 加 `debugMonitor` 開關，預設關閉 |

**不修改**：`EnemyMoveState`、`EnemyWanderState`、`EnemyAttackState`、`EnemySkillState`、所有 Prefab、所有 ScriptableObject。

---

**實作步驟（小而可驗證）**

1. **EnemyController.cs**
   - 在 `[Header("Targeting")]` 區塊新增：
     ```csharp
     [Tooltip("EnemyIdleState 的索敵間隔（秒）。降低每幀 OverlapSphere 呼叫次數。")]
     [Min(0.1f)] public float searchInterval = 0.3f;
     ```
   - 在現有 `OnValidate()` 中（或新建 `private void OnValidate()`）加入：
     ```csharp
     if (escapeRange <= detectionRange) escapeRange = detectionRange + 2f;
     if (attackHitTime > attackDuration)  attackHitTime = attackDuration * 0.5f;
     ```
   - 同步刪除 Start() 中相同邏輯的 `Debug.LogWarning + runtime fix`（已有 OnValidate 保護，保留 `Debug.LogError` 的 Collider 檢查不動）。
   - 加 `ponytail:` 注解說明 searchInterval 的上限：「高值對靜止大量敵人友好，但 0.1s 已是場上最多 12 隻的合理下限。」

2. **EnemyIdleState.cs**
   - 加入私有欄位 `private float _searchTimer`
   - 在 `Enter()` 重置 `_searchTimer = ctx.searchInterval`（進 Idle 立即索敵一次）
   - 在 `Update()` 的 `if (ctx.target == null)` 分支內：
     ```csharp
     _searchTimer += Time.deltaTime;
     if (_searchTimer >= ctx.searchInterval)
     {
         _searchTimer = 0f;
         ctx.FindNearestTarget();
     }
     ```
   - 其餘邏輯（`_idleTimer`、`WanderState` 切換）不變。

3. **EnemySkillData.cs**
   - `OnValidate` 補充：
     ```csharp
     if (cooldown < 0.1f) cooldown = 0.1f;
     if (initialDelay < 0f) initialDelay = 0f;
     ```

4. **ReporterMonitor.cs**
   - 在 `[Header` 區域新增：
     ```csharp
     [Tooltip("開啟後輸出監視過濾原因（只供除錯，正式遊玩請關閉）")]
     [SerializeField] private bool debugMonitor = false;
     ```
   - 將 L137、L145、L154、L161、L173、L223、L233 的 `Debug.Log(...)` 包入 `if (debugMonitor)` guard（`Debug.LogError` 保留，不加 guard）。

---

**可能風險與回歸影響**

| 風險 | 評估 | 緩解方式 |
|---|---|---|
| `searchInterval = 0.3s` 讓敵人反應略慢 | 低風險：Idle 狀態本身已有 wanderInterval 緩衝，0.3s 在正常遊玩下不可察覺 | 欄位 Inspector 可調，playtest 後調整 |
| Start() 距離修正刪除後，若 OnValidate 在 PlayMode 不觸發 | 低風險：Unity OnValidate 在 Inspector 改值時觸發，Editor 與 PlayMode 進入前均執行 | 保留 Start() 的簡短 `if` check（不含 LogWarning，僅靜默修正），雙重保護 |
| Reporter.prefab 新增 `debugMonitor` 欄位後序列化 | 無風險：新 SerializeField 預設 false，舊 prefab 不讀此欄位時等同 false | — |
| EnemySkillData OnValidate 修改 cooldown 最小值 | 無風險：現有 B_Stun(8s)、B_Damage(12s)、E_Stun(10s) 均遠超 0.1s | — |

---

**靜態檢查方式**

- `grep -n "FindNearestTarget" Assets/Scripts/Enemy/EnemyIdleState.cs` 結果應只在計時分支出現，不在裸 `if (ctx.target == null)` 直接呼叫。
- `grep -n "OnValidate" Assets/Scripts/Enemy/EnemyController.cs` 應找到含 `escapeRange` 驗證的方法。
- `grep -n "debugMonitor" Assets/Scripts/Enemy/ReporterMonitor.cs` 應覆蓋所有 `Debug.Log`（非 `Debug.LogError`）。
- Unity Console 無新增 compile error。
- `Assets/Prefabs/Reporter.prefab`、`Assets/Prefabs/Boss_ .prefab`、`Assets/Prefabs/Elite_.prefab` 的 GUID 不變（不修改 Prefab，不需確認）。

---

**Unity Play Mode 驗收案例與預期結果**

1. **索敵節流**：場景中放 3 隻 Volunteer，Console 開 Deep Profile。確認 `FindNearestTarget` 呼叫頻率從每幀降至約每 0.3s 一次；敵人仍能正常偵測並追擊選民。
2. **OnValidate 距離關係**：在 Inspector 將 `escapeRange` 設為小於 `detectionRange` 的值（如 3），儲存後 `escapeRange` 應自動修正為 `detectionRange + 2`，Console 不出現 LogWarning。
3. **debugMonitor 預設關閉**：在記者關卡正常通關，Console 不出現 `[ReporterMonitor] 過濾：` 字樣。將 `debugMonitor` 勾起後，過濾原因重新出現。
4. **技能冷卻不受影響**：Task 07 的 Boss/Elite 技能排程行為不因本次修改改變（Tech Regression：Boss 兩招輪流，Elite 一招，debugSkillScheduling 開關仍有效）。

---

**完成條件**

- 上述四個 Play Mode 案例通過。
- `grep -rn "FindNearestTarget" Assets/Scripts/Enemy/EnemyIdleState.cs` 只出現在計時分支。
- 沒有新 compile error。
- 更新進度表。

**停止點**：若 OnValidate 搬移造成任何現有 Prefab 資料丟失，立即回報並回滾，不繼續。

#### 09｜完整關卡回歸與交接

依賴：01–08；前項待驗證必須列出，不能假裝已通過。
必讀：本文件進度、修改清單及相關任務／場景配置。
驗收情境：
- 記者會：指定記者全數退場才過關；混有志工不影響。
- 搶票：志工可反覆破防但不死亡；干擾後能恢復拉票。
- 精英：無敵但可控，達票數門檻後仍走正確下一關。
- Boss：兩招有應對空間；票數成功、超時失敗與玩家歸零各走正確結局，無重複結算。
- 跨場景及重開本局：計數、訂閱、冷卻、目標與 UI 無殘留。
- 結算／對話／非 Active 階段：不再造成監視處罰或戰鬥傷害。
- 設定異常：空技能、空白名單、缺目標 Prefab、無 NavMesh 都有明確可診斷結果。
交付：實際使用的 Prefab／SO／場景清單、可調數值對照、測試證據、未解問題；更新直接受本次規則影響的舊文件，保留歷史標示。
停止點：若需平衡調整，以小範圍任務回報；不繼續開發延期功能。

## 5. Inspector 編輯責任

| 位置 | 設定 | 不應存放 |
|---|---|---|
| 角色 Prefab | 角色、HP／耐久、移速、受擊規則、距離、引用 | 跨場景戰役資料 |
| 技能 SO | 冷卻、前搖、形狀、效果數值、VFX | 每隻敵人的冷卻計時器 |
| 記者元件／設定 | 監視清單、傷害白名單、距離、處罰 | 無界事件歷史 |
| 任務 SO／SpawnConfig | 指定目標、生成、門檻與時限 | 全場敵人死亡就是成功的假設 |

更改共享 SO 會影響所有引用者，調整單一角色時使用獨立配置；不要在遊玩時把暈眩或冷卻寫回共享資產。測試數值需記錄，不能因數值可調就跳過驗收。

## 6. 進度與交接

全部初始狀態均為待開始。每次只更新正在做的一列。

| 任務 | 狀態 | Commit | 程式檢查 | Unity 驗收 | 阻礙／下一步 |
|---|---|---|---|---|---|
| 01 志工耐久 | 待開始 | — | 未執行 | 未執行 | 從此開始 |
| 02 行為優先權 | 程式完成，待驗收 | 44b78f6 + 本次修正 | 新增精英／Boss 的玩家技能暈眩與 layer 回歸測試（待 Unity 執行） | 待 Unity Play Mode／Test Runner 驗收 | Boss／精英技能 SO 缺少資產，留 Task 07 設定 |
| 03A 停止距離對齊 | 完成（使用者 Unity 實測通過） | 6f3d57b（程式）+ d191d80（Prefab） | EnemyMoveState 停步改 attackHitRadius+offset.z；Volunteer/Elite_/Boss_ closeRangeRadius 由 1.5→1 對齊 attackHitRadius=1；player/Reporter 未動 | 使用者已在 Unity Play Mode 實測確認 | attackRange 欄位保留不再控制停步（ponytail 標記） |
| 03B 同攻擊去重 | 完成（使用者 Unity Play Mode 實測通過） | ecca322b5e671b5e6c0866fe5468a03011f3db0d | PlayerAttack.PerformAttack 與 EnemyController.PerformAttackHit 各加 HashSet 去重；DogezaSkill 已有去重；靜態確認邏輯正確 | 使用者已確認多 Collider 不造成同次攻擊重複結算 | — |
| 03B 補：Boss/Elite 暈眩不重設 | 完成（使用者 Unity Play Mode 實測通過） | e4d77ba | TakeDamage isBossOrInvincible 分支加入 is EnemyStunState 保護；修正錯誤 ponytail 註解 | 使用者已確認首次暈眩正常、重複攻擊不重設、暈眩結束可再暈眩 | — |
| 03C 硬寫數值序列化 | 完成（使用者 Unity Play Mode 實測通過） | c210189cc468d5e83387f092100ec46db08f75c9 | PlayerAttack 新增 enemyHitDamage=10、enemyHitStunDuration=1f；EnemyController 新增 playerHitDamage=1f；Inspector Tooltip 區分選民影響與角色傷害 | 使用者已確認 enemyHitDamage/enemyHitStunDuration/playerHitDamage 改值後行為跟著變動 | — |
| 04 Boss 資產與結算 | 完成（使用者 Unity Play Mode 實測通過） | 8bf4037 + e9d35de + c4459df + 128113a | MissionTracker FinalBoss 票數輪詢＋時限敗選；GameplayState FinalBoss LevelTimer；BattleFlowController FinalBoss 清場；NavMesh Warp 回傳值檢查；HandleObjectiveResolved 改為 pending+delay-one-frame 競態仲裁 | 使用者已在 Unity Play Mode 實測確認 | — |
| 05 記者與清場 | ✅ 完成（使用者 Unity Play Mode 實測回報正常） | be9fefb + 95cc10d + d6b4821 + 4924426 | EnemyHitInfo/EnemyHitSourceType；TakeDamage 白名單；ReporterMissionTracker；EnemySpawner Register/FinishRegistration；MissionTracker isReporterMission 分流；Die() _isDead guard；M_EliminateAll hudLayout 修正；S_EliminateAll 移除 Volunteer；Timer 預設 inactive；.meta 補交 | 使用者已確認 3 隻 Reporter 正常計數過關，計時器顯示問題修正後通過 | Task 06 已可開始 |
| 06 記者監視 | 程式完成，待 Unity Play Mode 驗收 | f3a37cc + 1dd55d1 + eff70b4 + c925106 | ReporterFollowState（跟隨候選人）；ReporterMonitor（監視+處罰+去重）；SkillCastInfo；BattleEventManager.OnSkillCast；SkillState/EnemySkillState 觸發點；EnemyController Reporter 分流初始化；Die() 停止 Agent；EnemyStunState 恢復到 FollowState；06-Fix：PlayerHealthSystem 加入 S0 GameDB GO；靜態事件改具名解除訂閱；候選人共享冷卻；死亡後 Update/FixedUpdate guard；Phase 嚴格限定 Active；Domain Reload 保護 | 待使用者 Unity Play Mode 驗收 | allowMonitorInNonePhase 預設關閉；monitoredSkills 需 Inspector 設定 |
| 07 精英／Boss 技能 | 程式完成（排程修正後），待 Unity Play Mode 驗收 | e443168 + 60c9843 + 4f6b889 + 0260c1a | 07-Fix：activeCombatTime 時間戳模型（冷卻在所有狀態推進）；首次延遲修正（initialDelay 不疊加 cooldown）；Round-robin 防第二招飢餓；debugSkillScheduling Inspector 開關；0260c1a 修正 InitializeMultiSkillSystem 懸空 else 造成括號錯位 | 待使用者 Unity Play Mode 驗收（精英一招、Boss 兩招輪流、不空放、被打斷） | Unity 驗收尚未回報；Task 08 靜態部分已可並行規劃 |
| 08 設定與效能 | 規劃完成，待 ChatGPT 審查確認後實作 | — | 未執行 | 未執行 | Task 07 Unity 驗收尚待使用者執行；Task 08 靜態整理部分不阻擋（見 Task 08 規劃） |
| 09 關卡回歸 | 待開始 | — | 未執行 | 未執行 | 依賴 01–08 |

回報格式：
- 本次編號／狀態：
- 修改檔案與理由：
- Inspector 設定（物件／欄位／值）：
- 實際執行的檢查與結果：
- 尚未執行的 Unity 驗收：
- Commit／阻礙／下一步：

本地 AI 啟動提示：
> 請閱讀 docs/plans/enemy-behavior-implementation-plan.md 與適用的 AGENTS.md。這次只執行任務 01。先確認工作分支與未提交變更，再只讀必要檔案與相關呼叫者。完成最小修改、必要檢查及進度紀錄後停止；不要執行 02。未執行 Unity 的項目標成待驗收，列出我需要操作的 Inspector 與 Play Mode 步驟。不要提交與本步無關的使用者變更。

後續將編號替換成指定步驟即可。每步一個可回溯 commit 是預設；若程式完成但等待手動驗收，標示「待驗收」而不是完成。不要自動合併 PR 或推進下一階段。

### Active Delivery Target

本計畫 Task 01–09 全部屬於同一個實作工作。

- Active PR: #3
- PR Head Branch: `docs/enemy-behavior-plan-20261006`
- Base Branch: `6000.3.17f1_全新的生命`

所有本計畫後續 commit 必須 push 到
`docs/enemy-behavior-plan-20261006`
並持續更新既有 PR #3。

禁止自行：
- 建立新的 implementation branch
- 建立新的 Pull Request
- 關閉或取代 PR #3
- 將任務拆成其他 PR

除非使用者明確要求。

若目前 local branch 不是上述 PR head branch：
停止實作，先切換／同步正確 branch；
不得以建立新 PR 作為替代方案。
