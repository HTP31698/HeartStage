using UnityEngine;

public class DancingMachineSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "DancingMachineSkill";
        skillId = 31222;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
