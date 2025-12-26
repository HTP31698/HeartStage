using UnityEngine;

public class FaceGeniusSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "FaceGeniusV2";
        skillId = 31203;
        sfxName = SoundName.SFX_FaceGenius_Skill;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}