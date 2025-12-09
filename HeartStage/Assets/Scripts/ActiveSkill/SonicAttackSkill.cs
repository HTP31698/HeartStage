using UnityEngine;

public class SonicAttackSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "SonicAttack";
        skillId = 31204;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}