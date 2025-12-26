using UnityEngine;

public class FairySkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "FairyV2";
        skillId = 31213;
        sfxName = SoundName.SFX_Fairy_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}