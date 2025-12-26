using UnityEngine;

public class MeteorMelodySkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MeteorMelody";
        skillId = 31210;
        sfxName = SoundName.SFX_Meteor_Melody_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}