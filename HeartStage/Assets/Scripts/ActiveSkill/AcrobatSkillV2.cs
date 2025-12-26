using UnityEngine;

public class AcrobatSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AcrobatSkillV2";
        skillId = 31217;
        sfxName = SoundName.SFX_Acrobat_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
