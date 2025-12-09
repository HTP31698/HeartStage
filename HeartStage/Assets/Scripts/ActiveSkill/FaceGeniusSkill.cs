using UnityEngine;

public class FaceGeniusSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "FaceGenius";
        skillId = 31202;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}