using UnityEngine;

public class FaceGeniusSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "FaceGeniusV2";
        skillId = 31203;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}