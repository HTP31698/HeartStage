using UnityEngine;

public class MaknaeOnTopSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MaknaeOnTopSkillV2";
        skillId = 31219;
        sfxName = SoundName.SFX_MaknaeOnTop_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
