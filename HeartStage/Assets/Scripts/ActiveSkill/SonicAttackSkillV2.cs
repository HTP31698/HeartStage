using UnityEngine;

public class SonicAttackSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "SonicAttackV2";
        skillId = 31205;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}