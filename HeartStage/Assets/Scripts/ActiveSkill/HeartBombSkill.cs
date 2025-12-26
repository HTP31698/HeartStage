using UnityEngine;

public class HeartBombSkill : BaseProjectileSkill
{
    private void Awake()
    {
        poolId = "HeartBomb";
        skillId = 31208;
        sfxName = SoundName.SFX_HeartBomb_Skill;
    }

    protected override Vector3 GetStartPosition() => GetCenterInMonsters();
    protected override Vector3 GetDirection() => Vector3.zero;

    protected override Vector3 GetCenterInMonsters()
    {
        var objs = GameObject.FindGameObjectsWithTag(Tag.Monster);
        if (objs.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        // 스킬 방향
        Vector3 dir = base.GetDirection().normalized;

        foreach (var obj in objs)
        {
            // 몬스터가 dir 방향 전방에 있는지 판단
            Vector3 toMonster = obj.transform.position - transform.position;

            // dot 값이 양수: dir 방향 전방에 있음
            if (Vector3.Dot(dir, toMonster) > 0f)
            {
                sum += obj.transform.position;
                count++;
            }
        }

        return count == 0 ? Vector3.zero : sum / count;
    }
}