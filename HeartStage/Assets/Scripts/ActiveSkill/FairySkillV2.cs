using UnityEngine;

public class FairySkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "FairyV2";
        skillId = 31213;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}