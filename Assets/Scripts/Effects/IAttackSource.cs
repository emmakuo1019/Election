using System;

public interface IAttackSource
{
    float AttackRange { get; }
    float AttackAngle { get; }

    event Action<float, float> OnAttackShapeChanged;
}