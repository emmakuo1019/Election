using UnityEngine;

public class AttackState : IPlayerState
{
    private readonly PlayerStateMachine _ctx;
    private float attackDuration = 0.4f; // 對應攻擊動畫的長度
    private float attackTimer;

    public AttackState(PlayerStateMachine ctx) => _ctx = ctx;

    public void Enter()
    {
        // 進入普攻時，重置計時器
        attackTimer = 0f;

        // 立刻將玩家的移動速度鎖定為 0 (透過不再 Update 中給予任何位移即可達成)
        // 為了確保物理上完全靜止，也可以顯式地歸零（若有受重力影響，這裡先忽略水平移動）
        _ctx.CharCon.Move(Vector3.zero);

        // 使用獨立出來的動畫控制器播放對應方向的攻擊動畫
        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayAttackAnimation(_ctx.lastFacingDirection);
        }

        // 執行言語攻擊判定
        _ctx.PlayerAttack?.PerformSpeech();
    }

    public void Update()
    {
        // 絕對硬直：禁止讀取任何 WASD、衝刺 (Dash) 的 Input 偵測
        
        // 狀態內計時器累加
        attackTimer += Time.deltaTime;

        // 與 Animator 箭頭同步安全退出
        if (attackTimer >= attackDuration)
        {
            _ctx.ChangeState(new IdleState(_ctx));
        }
    }

    public void Exit()
    {
        // 狀態退出時的清理，Animator 已經透過箭頭回到 Idle 動畫，不需要在這裡額外強制重置動畫狀態
    }
}
