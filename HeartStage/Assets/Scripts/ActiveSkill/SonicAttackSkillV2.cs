using UnityEngine;

public class SonicAttackSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "SonicAttackV2";
        skillId = 31205;
        sfxName = SoundName.SFX_SonicAttack_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}