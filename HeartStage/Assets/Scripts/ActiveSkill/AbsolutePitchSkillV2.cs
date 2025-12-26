using UnityEngine;

public class AbsolutePitchSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AbsolutePitchSkillV2";
        skillId = 31221;
        sfxName = SoundName.SFX_AbsolutePitch_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
