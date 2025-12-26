using UnityEngine;

public class AbsolutePitchSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AbsolutePitchSkill";
        skillId = 31220;
        sfxName = SoundName.SFX_AbsolutePitch_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
    // test
}
