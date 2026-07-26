using UnityEngine;

public class StunState : IState
{
    private readonly PlayerController _ctx;
    private readonly float _duration;
    private float _endTime;
    private GameObject _stunVfxInstance; // 暈眩 VFX 實例，Exit 時銷毀

    public StunState(PlayerController ctx, float duration)
    {
        _ctx = ctx;
        _duration = duration;
    }

    public void Enter()
    {
        _endTime = Time.time + _duration;

        // 生成暈眩 VFX，設為子物件使其跟隨玩家
        if (_ctx.stunVfxPrefab != null)
        {
            Vector3 spawnPos = _ctx.transform.position + _ctx.stunVfxOffset;
            _stunVfxInstance = Object.Instantiate(_ctx.stunVfxPrefab, spawnPos, Quaternion.identity);
            _stunVfxInstance.transform.SetParent(_ctx.transform, worldPositionStays: true);
        }

        Debug.Log($"[StunState] Enter — duration: {_duration}");
    }

    public void Update()
    {
        if (Time.time >= _endTime)
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        // 離開暈眩狀態時銷毀 VFX
        if (_stunVfxInstance != null)
        {
            Object.Destroy(_stunVfxInstance);
            _stunVfxInstance = null;
        }

        Debug.Log("[StunState] Exit");
    }
}
