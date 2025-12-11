using UnityEngine;

public class MeteorMelodySkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "MeteorMelody";
        skillId = 31210;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}