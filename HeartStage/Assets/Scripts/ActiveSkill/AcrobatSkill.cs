using UnityEngine;

public class AcrobatSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AcrobatSkill";
        skillId = 31216;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
