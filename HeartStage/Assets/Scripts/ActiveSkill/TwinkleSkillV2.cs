using UnityEngine;

public class TwinkleSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "TwinkleSkillV2";
        skillId = 31215;
        sfxName = SoundName.SFX_Twinkle_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
