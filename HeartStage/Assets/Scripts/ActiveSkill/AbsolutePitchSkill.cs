using UnityEngine;

public class AbsolutePitchSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "AbsolutePitchSkill";
        skillId = 31220;
    }

    protected override Vector3 GetStartPosition() => transform.position;
}
