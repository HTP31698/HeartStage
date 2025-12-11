using UnityEngine;

public class FairySkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "Fairy";
        skillId = 31212;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}