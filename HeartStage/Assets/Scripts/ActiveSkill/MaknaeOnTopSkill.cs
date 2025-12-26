using UnityEngine;

public class MaknaeOnTopSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MaknaeOnTopSkill";
        skillId = 31218;
        sfxName = SoundName.SFX_MaknaeOnTop_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
