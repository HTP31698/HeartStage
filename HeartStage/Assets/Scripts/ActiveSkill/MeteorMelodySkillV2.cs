using UnityEngine;

public class MeteorMelodySkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MeteorMelodyV2";
        skillId = 31211;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}