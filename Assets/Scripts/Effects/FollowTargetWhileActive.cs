using UnityEngine;

public class FollowTargetWhileActive : MonoBehaviour
{
    private Transform target;
    private Vector3 localOffset;
    private bool matchRotation;
    private PlayerStateMachine playerStateMachine;

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.TransformPoint(localOffset);

        if (matchRotation)
            transform.rotation = GetTargetRotation();
    }

    private void OnDisable()
    {
        target = null;
        localOffset = Vector3.zero;
        matchRotation = false;
        playerStateMachine = null;
    }

    public void Bind(Transform targetTransform, Vector3 offset, bool followRotation)
    {
        target = targetTransform;
        localOffset = offset;
        matchRotation = followRotation;
        playerStateMachine = targetTransform != null ? targetTransform.GetComponent<PlayerStateMachine>() : null;
        LateUpdate();
    }

    private Quaternion GetTargetRotation()
    {
        if (playerStateMachine != null && playerStateMachine.LastMoveDirection.sqrMagnitude > 0.001f)
            return Quaternion.LookRotation(playerStateMachine.LastMoveDirection.normalized, Vector3.up);

        return target.rotation;
    }
}
