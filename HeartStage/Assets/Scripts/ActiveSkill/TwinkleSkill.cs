using UnityEngine;

public class TwinkleSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "TwinkleSkill";
        skillId = 31214;
        sfxName = SoundName.SFX_Twinkle_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
