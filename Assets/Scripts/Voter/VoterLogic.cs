using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VoterLogic : MonoBehaviour
{
    public StateMachine StateMachine { get; private set; }
    
    public VoterData Data { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public VoterVisuals Visuals { get; private set; }

    public bool IsGameActive { get; set; } = true;
    public bool HasUsableNavMeshAgent { get; private set; }

    private const float NavMeshSnapDistance = 3f;

    [Header("隨機移動")]
    public float wanderRadius = 6f;
    public float wanderIntervalMin = 2f;
    public float wanderIntervalMax = 5f;
    public float moveSpeed = 1.2f;

    [Header("深色選民追擊")]
    public float darkApproachStoppingDistance = 1.5f;
    public float darkDestinationRefreshInterval = 0.25f;

    [Header("擊退設定")]
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.25f;

    public bool CanReceiveSkillEffect => IsGameActive && Data != null;

    private void Awake()
    {
        Data = GetComponent<VoterData>();
        Agent = GetComponent<NavMeshAgent>();
        Visuals = GetComponent<VoterVisuals>();
        
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerTransform = playerObject != null ? playerObject.transform : null;

        Data?.InitializeFromConfig();

        if (Agent != null)
        {
            Agent.speed = Data != null ? Data.MoveSpeed : moveSpeed;
            Agent.angularSpeed = 0f;
            Agent.updateRotation = false;
            HasUsableNavMeshAgent = TryInitializeNavMeshAgent();
        }

        StateMachine = new StateMachine();
    }

    private void OnEnable()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
        if (Data != null)
        {
            Data.OnConversionSuccess += HandleConversionSuccess;
            Data.OnConversionLost += HandleConversionLost;
        }
    }

    private void OnDisable()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
        }
        if (Data != null)
        {
            Data.OnConversionSuccess -= HandleConversionSuccess;
            Data.OnConversionLost -= HandleConversionLost;
        }
    }

    private void Start()
    {
        if (HasUsableNavMeshAgent)
        {
            StateMachine.Initialize(new VoterIdleState(this));
        }
    }

    private void Update()
    {
        if (!IsGameActive || Data == null || Agent == null || !HasUsableNavMeshAgent || !Agent.isOnNavMesh)
        {
            return;
        }

        UpdateLoyaltyDecay();
        StateMachine.CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    public void RefreshMovementSpeed()
    {
        if (Agent != null)
        {
            Agent.speed = Data != null ? Data.MoveSpeed : moveSpeed;
        }
    }

    public bool ApplySkillEffect(IVoterSkillEffect effect)
    {
        if (!CanReceiveSkillEffect) return false;
        if (effect == null) return false;
        return effect.ApplyTo(this);
    }

    private bool TryInitializeNavMeshAgent()
    {
        if (Agent == null) return false;
        if (Agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas))
        {
            bool previousEnabled = Agent.enabled;
            Agent.enabled = false;
            transform.position = hit.position;
            Agent.enabled = previousEnabled;

            if (Agent.isOnNavMesh) return true;
        }

        Debug.LogWarning($"[VoterLogic] {name} 找不到可用 NavMesh，已停用 NavMeshAgent。");
        Agent.enabled = false;
        return false;
    }

    // ==========================================
    // 外部觸發機制 (Public API)
    // ==========================================

    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default, bool allowSpread = true)
    {
        if (!IsGameActive || Data == null) return;

        int finalAmount = amount * Mathf.Max(1, 1 + Data.EmotionLabelCount);
        Data.CurrentPosition = Mathf.Clamp(
            Data.CurrentPosition + finalAmount,
            -Data.MaxSupportValue,
            Data.MaxSupportValue);

        UpdateConversionState(allowSpread);
        
        // 無論有沒有 NavMeshAgent，被影響時都統一觸發受擊閃爍 (改成紅色才看得出差異)
        Visuals?.TriggerHitFlash(Color.red, 0.15f);

        if (HasUsableNavMeshAgent && attackerPosition != default)
        {
            StateMachine.ChangeState(new VoterHitState(this, attackerPosition));
        }
        else
        {
            Visuals?.PlayHitAnimation();
        }
    }

    public void ApplyTimedStun(float stunTime)
    {
        if (stunTime <= 0f) return;
        StateMachine.ChangeState(new VoterStunState(this, stunTime));
    }

    public void TriggerCheer(Vector3 lookTarget = default)
    {
        if (!IsGameActive) return;
        StateMachine.ChangeState(new VoterCheerState(this, lookTarget));
    }

    // ==========================================
    // 動畫事件接收口 (Animation Event Receiver)
    // ==========================================
    public void TriggerAnimationEnd()
    {
        StateMachine.CurrentState?.AnimationFinishTrigger();
    }

    // ==========================================
    // 內部資料更新邏輯
    // ==========================================

    private void UpdateConversionState(bool allowSpread)
    {
        int oldSide = Data.ConvertedSide;
        int newSide = Data.EvaluateSideFromPosition();

        // 寫入新的陣營，會自動觸發 VoterData 內的事件與狀態更新
        Data.ConvertedSide = newSide;

        if (oldSide != newSide)
        {
            Data.loyalty = 1f;
        }

        if (oldSide != newSide)
        {
            VoteManager.Instance?.ApplyAlignmentChange(oldSide, newSide);

            if (newSide == VoterData.PlayerSideSign && oldSide != VoterData.PlayerSideSign)
            {
                PlayerMPSystem.Instance?.RecoverMP(1);

                if (allowSpread)
                {
                    SpreadInfluenceToNearbyVoters();
                }
            }
        }
    }

    public void RevertToNeutral()
    {
        int oldSide = Data.ConvertedSide;
        Data.CurrentPosition = 0;
        Data.ConvertedSide = VoterData.NeutralSideSign;
        Data.loyalty = 1f;

        VoteManager.Instance?.ApplyAlignmentChange(oldSide, VoterData.NeutralSideSign);
    }

    private void UpdateLoyaltyDecay()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        if (effects == null || !Data.isConverted || effects.LoseControlRate <= 0f)
        {
            return;
        }

        Data.loyalty = Mathf.Clamp01(Data.loyalty - effects.LoseControlRate * Time.deltaTime);

        if (Data.loyalty <= 0f)
        {
            RevertToNeutral();
        }
    }

    private readonly Collider[] spreadHitBuffer = new Collider[32];
    private void SpreadInfluenceToNearbyVoters()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        if (effects == null || effects.SpreadRadius <= 0f) return;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, effects.SpreadRadius, spreadHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = spreadHitBuffer[i];
            if (hit == null) continue;

            VoterLogic nearbyVoter = hit.GetComponentInParent<VoterLogic>();
            if (nearbyVoter == null || nearbyVoter == this || nearbyVoter.Data == null) continue;
            if (nearbyVoter.Data.IsPlayerAligned) continue;

            nearbyVoter.OnInfluence(1, true, transform.position, false);
        }
    }

    public void ForceConvertToPlayer()
    {
        if (Data == null) return;
        int requiredInfluence = Data.MaxSupportValue - Data.CurrentPosition;
        if (requiredInfluence <= 0) return;
        OnInfluence(requiredInfluence, true, transform.position);
    }

    public void ConvertColdIdentityToEmotion()
    {
        Data?.ConvertColdIdentityToEmotion();
    }

    // ==========================================
    // 轉化狀態事件回呼 (Event Handlers)
    // ==========================================

    private void HandleConversionSuccess(int side)
    {
        // 顯示成功轉化的 UI
        Visuals?.ShowEmote(EmoteType.Success);

        // 如果目前處於 WaverState，強制切換回 Idle (或 Follow)
        if (StateMachine.CurrentState is VoterWaverState)
        {
            if (Data.ShouldFollowPlayer)
                StateMachine.ChangeState(new VoterFollowState(this));
            else
                StateMachine.ChangeState(new VoterIdleState(this));
        }
    }

    private void HandleConversionLost()
    {
        // 顯示流失的 UI
        Visuals?.ShowEmote(EmoteType.Lost);

        // 如果目前處於 WaverState，強制切回 Idle
        if (StateMachine.CurrentState is VoterWaverState)
        {
            StateMachine.ChangeState(new VoterIdleState(this));
        }
    }

    // ==========================================
    // 簡單處理離場 bug，未來將替換為 VoterExitState
    // ==========================================
    public void BeginExitMovement(Vector3 destination)
    {
        IsGameActive = false;
        
        // 簡單做法：直接叫 Agent 走過去。未來會由獨立的 VoterExitState 來負責
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = false;
            Agent.SetDestination(destination);
        }
    }

    private void OnGameEnd()
    {
        IsGameActive = false;
        // 遊戲結束時強制進入離開狀態或待機
        StateMachine.ChangeState(new VoterIdleState(this)); 
    }

/* 
=========================================================
[開發筆記] 未來特殊狀態擴充指南 (FleeState & ExitState)
=========================================================
由於本次 Demo 著重於穩定性，選民機制暫不處理逃跑，且退場機制使用簡單寫法。
若未來發佈後需要擴充，請依照既有狀態機架構新增以下狀態：

1. VoterFleeState (逃跑狀態)
   - 觸發時機：當特定陣營技能生效，或受驚嚇時，由外部呼叫 ChangeState 進入。
   - 實作概念：在 Enter() 計算反方向目標點 (如：玩家位置的反向延長線)，呼叫 Agent.SetDestination()。
   - 結束條件：在 Update() 檢查跑到安全距離後，切回 Idle 或是 Exit。

2. VoterExitState (離場狀態)
   - 觸發時機：遊戲倒數結束後，統一觸發向指定地點移動並退場。
   - 實作概念：取代原本 `BeginExitMovement()` 中的臨時寫法。Enter() 時設定 Agent.SetDestination(destination)，並在 Update() 中檢查是否已到達目的地。
   - 結束條件：當接近目的地 (或到達) 時，執行物件回收或 Destroy，將選民移出遊戲。

※ 註：遊戲設定選民不會死亡，因此不需要實作 VoterDeadState。
設計原則：堅持 OCP，不要在主腳本寫判斷，一律實作新 State 並由外部觸發切換！
=========================================================
*/
}
