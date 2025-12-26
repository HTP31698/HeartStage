using UnityEngine;

public class ReverseCharmSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "ReverseCharm";
        skillId = 31206;
        sfxName = SoundName.SFX_ReverseCharm_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}