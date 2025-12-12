using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Scriptable Objects/MonsterData")]
public class MonsterData : ScriptableObject
{
    public int id;
    public string monsterName;
    public int monsterType;
    public int hp;
    public int att;
    public int attType;
    public float attackSpeed;
    public float attackMinRange; 
    public float attackMaxRange; 
    public int bulletSpeed;
    public float moveSpeed;
    public int minExp;
    public int maxExp;

    // 새로 추가된 필드들
    public int skillId1;
    public int skillId2;
    public int skillId3;
    public int itemId1;
    public int dropCount1;
    public int itemId2;
    public int dropCount2;
    public string prefab1;
    public string prefab2;

    public bool isInitialized = false;

    public MonsterCSVData ToCSVData()
    {
        var csvData = new MonsterCSVData
        {
            mon_id = id,
            mon_name = monsterName,
            mon_type = monsterType,
            hp = hp,
            atk_dmg = att,
            atk_type = attType,
            atk_speed = attackSpeed,
            atk_min_range = attackMinRange, 
            atk_max_range = attackMaxRange, 
            bullet_speed = bulletSpeed,
            speed = moveSpeed,
            skill_id1 = skillId1,
            skill_id2 = skillId2,
            skill_id3 = skillId3,
            min_level = minExp,
            max_level = maxExp,
            item_id1 = itemId1,
            drop_count1 = dropCount1,
            item_id2 = itemId2,
            drop_count2 = dropCount2,
            prefab1 = prefab1,
            prefab2 = prefab2,
        };

        return csvData;
    }

    public void UpdateData(MonsterCSVData csvData)
    {
        id = csvData.mon_id;
        hp = csvData.hp;
        att = csvData.atk_dmg;
        attType = csvData.atk_type;
        attackSpeed = csvData.atk_speed;
        attackMinRange = csvData.atk_min_range; // 변경
        attackMaxRange = csvData.atk_max_range; // 새로 추가
        bulletSpeed = csvData.bullet_speed;
        moveSpeed = csvData.speed;
        monsterName = csvData.mon_name;
        monsterType = csvData.mon_type;
        minExp = csvData.min_level;
        maxExp = csvData.max_level;

        // 새로운 필드들
        skillId1 = csvData.skill_id1;
        skillId2 = csvData.skill_id2;
        skillId3 = csvData.skill_id3;
        itemId1 = csvData.item_id1;
        dropCount1 = csvData.drop_count1;
        itemId2 = csvData.item_id2;
        dropCount2 = csvData.drop_count2;
        prefab1 = csvData.prefab1;
        prefab2 = csvData.prefab2;
    }

    public void InitFromCSV(int monsterId)
    {
        var monsterTable = DataTableManager.MonsterTable;
        if (monsterTable == null) return;

        var data = monsterTable.Get(monsterId);
        if (data == null) return;

        UpdateData(data);
        isInitialized = true;
    }

    public void Init(int monsterId)
    {
        var monsterTable = DataTableManager.MonsterTable;

        if (monsterTable == null)
            return;

        var data = monsterTable.Get(monsterId);
        if (data == null)
        {
            Debug.Log("몬스터 Id가 없습니다.");
            return;
        }

        UpdateData(data);
    }

    public MonsterCSVData ToTableData()
    {
        return new MonsterCSVData
        {
            mon_id = id,
            mon_name = monsterName,
            mon_type = monsterType,
            hp = hp,
            atk_dmg = att,
            atk_type = attType,
            atk_speed = attackSpeed,
            atk_min_range = attackMinRange, // 변경
            atk_max_range = attackMaxRange, // 새로 추가
            bullet_speed = bulletSpeed,
            speed = moveSpeed,
            skill_id1 = skillId1,
            skill_id2 = skillId2,
            skill_id3 = skillId3,
            min_level = minExp,
            max_level = maxExp,
            item_id1 = itemId1,
            drop_count1 = dropCount1,
            item_id2 = itemId2,
            drop_count2 = dropCount2,
            prefab1 = prefab1,
            prefab2 = prefab2,
        };
    }
}