using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator _animator;

    // ==========================================
    // 1. 動畫狀態 Hash 集中管理（效能優化與防呆）
    // ==========================================
    // 待機動畫四向 (暫時先全部接同一支 Male_idle 動畫，等美術圖補齊後再改回四方向)
    private static readonly int IdleUpHash = Animator.StringToHash("Male_idle");
    private static readonly int IdleDownHash = Animator.StringToHash("Male_idle");
    private static readonly int IdleLeftHash = Animator.StringToHash("Male_idle");
    private static readonly int IdleRightHash = Animator.StringToHash("Male_idle");

    // 走路動畫四向 (暫時先全部接同一支 Male_Move 動畫，等美術圖補齊後再改回四方向)
    private static readonly int WalkUpHash = Animator.StringToHash("Male_Move");
    private static readonly int WalkDownHash = Animator.StringToHash("Male_Move");
    private static readonly int WalkLeftHash = Animator.StringToHash("Male_Move");
    private static readonly int WalkRightHash = Animator.StringToHash("Male_Move");

    // 攻擊動畫四向 (暫時先全部接同一支 Male_Hit 動畫，等美術圖補齊後再改回四方向)
    private static readonly int AttackUpHash = Animator.StringToHash("Male_Hit");
    private static readonly int AttackDownHash = Animator.StringToHash("Male_Hit");
    private static readonly int AttackLeftHash = Animator.StringToHash("Male_Hit");
    private static readonly int AttackRightHash = Animator.StringToHash("Male_Hit");

    // 衝刺動畫四向 (暫時先全部接同一支 dash 動畫，等美術圖補齊後再改回四方向)
    private static readonly int DashUpHash = Animator.StringToHash("Male_Dash");
    private static readonly int DashDownHash = Animator.StringToHash("Male_Dash");
    private static readonly int DashLeftHash = Animator.StringToHash("Male_Dash");
    private static readonly int DashRightHash = Animator.StringToHash("Male_Dash");



    private void Awake()
    {
        // 改為 GetComponentInChildren，因為 Animator 通常掛在子物件 (如 PlayerSprite) 上
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            Debug.LogError("[PlayerAnimationController] 找不到 Animator 元件！請確保自身或子物件有 Animator。");
        }
    }

    private int _currentStateHash;

    /// <summary>
    /// 內部統一播放動畫的方法，防止同一個動畫在 Update 中被連續呼叫導致卡在第一幀
    /// </summary>
    private void ChangeAnimationState(int newHash)
    {
        // 若當前播放的動畫已經是目標動畫，則不重複呼叫 (防止動畫從頭播放)
        if (_currentStateHash == newHash) return;
        
        _animator.Play(newHash);
        _currentStateHash = newHash;
    }

    // ==========================================
    // 2. 動畫控制對外接口 (供狀態機呼叫)
    // ==========================================

    /// <summary>
    /// 播放移動動畫（依據傳入的最後面朝方向，精確判定播放上、下、左、右）
    /// </summary>
    /// <param name="facingDirection">玩家目前的 Vector2 朝向向量</param>
    public void PlayWalkAnimation(Vector2 facingDirection)
    {
        // 優先判定 Y 軸（上、下）
        if (Mathf.Abs(facingDirection.y) >= Mathf.Abs(facingDirection.x))
        {
            if (facingDirection.y > 0)
            {
                ChangeAnimationState(WalkUpHash);
            }
            else
            {
                ChangeAnimationState(WalkDownHash);
            }
        }
        // 次要判定 X 軸（左、右）
        else
        {
            if (facingDirection.x < 0)
            {
                ChangeAnimationState(WalkLeftHash);
            }
            else
            {
                ChangeAnimationState(WalkRightHash);
            }
        }
    }

    /// <summary>
    /// 播放待機動畫（依據傳入的最後面朝方向，精確判定播放上、下、左、右待機）
    /// </summary>
    public void PlayIdleAnimation(Vector2 facingDirection)
    {
        if (Mathf.Abs(facingDirection.y) >= Mathf.Abs(facingDirection.x))
        {
            if (facingDirection.y > 0) ChangeAnimationState(IdleUpHash);
            else ChangeAnimationState(IdleDownHash);
        }
        else
        {
            if (facingDirection.x < 0) ChangeAnimationState(IdleLeftHash);
            else ChangeAnimationState(IdleRightHash);
        }
    }

    /// <summary>
    /// 播放攻擊動畫（依據傳入的最後面朝方向，精確判定播放上、下、左、右）
    /// </summary>
    /// <param name="facingDirection">玩家目前的 Vector2 朝向向量</param>
    public void PlayAttackAnimation(Vector2 facingDirection)
    {
        if (Mathf.Abs(facingDirection.y) >= Mathf.Abs(facingDirection.x))
        {
            if (facingDirection.y > 0)
            {
                ChangeAnimationState(AttackUpHash);
            }
            else
            {
                ChangeAnimationState(AttackDownHash);
            }
        }
        else
        {
            if (facingDirection.x < 0)
            {
                ChangeAnimationState(AttackLeftHash);
            }
            else
            {
                ChangeAnimationState(AttackRightHash);
            }
        }
    }

    /// <summary>
    /// 播放衝刺動畫（依據傳入的最後面朝方向，精確判定播放上、下、左、右）
    /// </summary>
    /// <param name="facingDirection">玩家目前的 Vector2 朝向向量</param>
    public void PlayDashAnimation(Vector2 facingDirection)
    {
        if (Mathf.Abs(facingDirection.y) >= Mathf.Abs(facingDirection.x))
        {
            if (facingDirection.y > 0)
            {
                ChangeAnimationState(DashUpHash);
            }
            else
            {
                ChangeAnimationState(DashDownHash);
            }
        }
        else
        {
            if (facingDirection.x < 0)
            {
                ChangeAnimationState(DashLeftHash);
            }
            else
            {
                ChangeAnimationState(DashRightHash);
            }
        }
    }
}