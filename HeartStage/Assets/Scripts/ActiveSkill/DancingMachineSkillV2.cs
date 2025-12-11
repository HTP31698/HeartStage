using UnityEngine;

public class DancingMachineSkillV2 : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "DancingMachineSkillV2";
        skillId = 31223;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
