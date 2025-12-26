using UnityEngine;

public class ReverseCharmSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "ReverseCharmV2";
        skillId = 31207;
        sfxName = SoundName.SFX_ReverseCharm_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}