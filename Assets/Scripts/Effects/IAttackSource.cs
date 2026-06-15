using System;
using UnityEngine;

public interface IAttackSource
{
    float AttackRange { get; }
    float AttackAngle { get; }
    Vector3 AttackDirection { get; }

    event Action<float, float> OnAttackShapeChanged;
}