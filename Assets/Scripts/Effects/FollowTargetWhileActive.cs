using UnityEngine;

public class FollowTargetWhileActive : MonoBehaviour
{
    private Transform target;
    private Vector3 localOffset;
    private bool matchRotation;
    private PlayerController playerController;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.TransformPoint(localOffset);

        if (matchRotation)
        {
            transform.rotation = GetTargetRotation();
        }
    }

    private void OnDisable()
    {
        target = null;
        localOffset = Vector3.zero;
        matchRotation = false;
        playerController = null;
    }

    public void Bind(Transform targetTransform, Vector3 offset, bool followRotation)
    {
        target = targetTransform;
        localOffset = offset;
        matchRotation = followRotation;
        playerController = targetTransform != null ? targetTransform.GetComponent<PlayerController>() : null;
        LateUpdate();
    }

    private Quaternion GetTargetRotation()
    {
        if (playerController != null && playerController.LastMoveDirection.sqrMagnitude > 0.001f)
        {
            return Quaternion.LookRotation(playerController.LastMoveDirection.normalized, Vector3.up);
        }

        return target.rotation;
    }
}
