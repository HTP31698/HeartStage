using UnityEngine;

public class ActiveSkillCreator : MonoBehaviour
{
    public static ActiveSkillCreator Instance;

    private GameObject sonicAttackPrefab;
    private string sonicAttackId = "SonicAttack";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // 액티브 스킬들 오브젝트 풀 생성
    private void Start()
    {
        sonicAttackPrefab = ResourceManager.Instance.Get<GameObject>(sonicAttackId);
        PoolManager.Instance.CreatePool(sonicAttackId, sonicAttackPrefab, 10, 30);
    }

    // 음파 공격 생성
    public void CreateSonicAttack(GameObject caster, SkillData data)
    {       
        if (caster == null)
            return;

        var projectileGo = PoolManager.Instance.Get(sonicAttackId);

        Vector3 startPos = caster.transform.position;
        Vector3 dir = Vector3.up;

        var proj = projectileGo.GetComponent<CharacterProjectile>();
        if (proj == null)
        {
            PoolManager.Instance.Release(sonicAttackId, projectileGo);
            return;
        }

        float speed = data.skill_speed;
        // 데미지계산로직변경해야함
        // int damage = data.skill_dmg;
        int damage = 0;

        proj.SetMissile(sonicAttackId, string.Empty, startPos, dir, speed, damage, PenetrationType.Penetrate);
    }
}