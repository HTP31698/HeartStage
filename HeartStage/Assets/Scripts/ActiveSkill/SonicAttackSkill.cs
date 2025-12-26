using UnityEngine;

public class SonicAttackSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "SonicAttack";
        skillId = 31204;
        sfxName = SoundName.SFX_SonicAttack_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}