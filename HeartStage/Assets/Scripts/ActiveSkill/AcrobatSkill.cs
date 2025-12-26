using UnityEngine;

public class AcrobatSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AcrobatSkill";
        skillId = 31216;
        sfxName = SoundName.SFX_Acrobat_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
