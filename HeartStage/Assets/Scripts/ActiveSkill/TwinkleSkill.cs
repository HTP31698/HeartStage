using UnityEngine;

public class TwinkleSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "TwinkleSkill";
        skillId = 31214;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
