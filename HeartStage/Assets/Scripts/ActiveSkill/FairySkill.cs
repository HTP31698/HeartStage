using UnityEngine;

public class FairySkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "Fairy";
        skillId = 31212;
        sfxName = SoundName.SFX_Fairy_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}