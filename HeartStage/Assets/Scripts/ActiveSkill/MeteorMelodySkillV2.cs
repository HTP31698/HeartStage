using UnityEngine;

public class MeteorMelodySkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MeteorMelodyV2";
        skillId = 31211;
        sfxName = SoundName.SFX_Meteor_Melody_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}