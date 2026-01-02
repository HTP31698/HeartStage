using UnityEngine;

/// <summary>
/// 무한 스테이지 데이터 ScriptableObject
/// CSV: InfiniteStageTable.csv
/// </summary>
[CreateAssetMenu(fileName = "InfiniteStageData", menuName = "HeartStage/InfiniteStageData")]
public class InfiniteStageData : ScriptableObject
{
    [Header("기본 정보")]
    public int stage_id;
    public string stage_name;

    [Header("제한")]
    public int daily_limit;      // 일일 플레이 제한
    public int deploy_limit;     // 배치 캐릭터 수

    [Header("스폰")]
    public int max_monsters;     // 필드 최대 몬스터
    public float spawn_interval; // 스폰 간격 (초)

    [Header("강화")]
    public float enhance_interval; // 강화 주기 (초)
    public float atk_mul;          // 공격력 배율
    public float hp_mul;           // 체력 배율
    public float speed_mul;        // 이동속도 배율

    [Header("기본 몬스터")]
    public int base_mon_id1;     // 기본 몬스터 1
    public int base_mon_id2;     // 기본 몬스터 2

    [Header("특수 몬스터 - 이속형")]
    public int fast_mon_id;
    public float fast_spawn_time;     // 첫 등장 시간
    public float fast_spawn_interval; // 반복 스폰 간격

    [Header("특수 몬스터 - 탱커형")]
    public int tank_mon_id;
    public float tank_spawn_time;
    public float tank_spawn_interval;

    [Header("특수 몬스터 - 강화형")]
    public int strong_mon_id;
    public float strong_spawn_time;
    public float strong_spawn_interval;

    [Header("보상")]
    public int reward_per_second; // 초당 보상
    public int reward_item_id;    // 보상 아이템 ID
    public int fan_per_second;    // 팬수 보상 주기 (초, 예: 10이면 10초당 1팬)

    [Header("배경/위치")]
    public string prefab;         // 배경 프리팹 이름
    public int stage_position;    // 스테이지 위치 (1=상, 2=중, 3=하)

    /// <summary>
    /// SO → CSV 데이터 변환
    /// </summary>
    public InfiniteStageCSVData ToCSVData()
    {
        return new InfiniteStageCSVData
        {
            stage_id = stage_id,
            stage_name = stage_name,
            daily_limit = daily_limit,
            deploy_limit = deploy_limit,
            max_monsters = max_monsters,
            spawn_interval = spawn_interval,
            enhance_interval = enhance_interval,
            atk_mul = atk_mul,
            hp_mul = hp_mul,
            speed_mul = speed_mul,
            base_mon_id1 = base_mon_id1,
            base_mon_id2 = base_mon_id2,
            fast_mon_id = fast_mon_id,
            fast_spawn_time = (int)fast_spawn_time,
            fast_spawn_interval = (int)fast_spawn_interval,
            tank_mon_id = tank_mon_id,
            tank_spawn_time = (int)tank_spawn_time,
            tank_spawn_interval = (int)tank_spawn_interval,
            strong_mon_id = strong_mon_id,
            strong_spawn_time = (int)strong_spawn_time,
            strong_spawn_interval = (int)strong_spawn_interval,
            reward_per_second = reward_per_second,
            reward_item_id = reward_item_id,
            fan_per_second = fan_per_second,
            prefab = prefab,
            stage_position = stage_position
        };
    }

    /// <summary>
    /// CSV 데이터로부터 SO 갱신
    /// </summary>
    public void FromCSVData(InfiniteStageCSVData csv)
    {
        stage_id = csv.stage_id;
        stage_name = csv.stage_name;
        daily_limit = csv.daily_limit;
        deploy_limit = csv.deploy_limit;
        max_monsters = csv.max_monsters;
        spawn_interval = csv.spawn_interval;
        enhance_interval = csv.enhance_interval;
        atk_mul = csv.atk_mul;
        hp_mul = csv.hp_mul;
        speed_mul = csv.speed_mul;
        base_mon_id1 = csv.base_mon_id1;
        base_mon_id2 = csv.base_mon_id2;
        fast_mon_id = csv.fast_mon_id;
        fast_spawn_time = csv.fast_spawn_time;
        fast_spawn_interval = csv.fast_spawn_interval;
        tank_mon_id = csv.tank_mon_id;
        tank_spawn_time = csv.tank_spawn_time;
        tank_spawn_interval = csv.tank_spawn_interval;
        strong_mon_id = csv.strong_mon_id;
        strong_spawn_time = csv.strong_spawn_time;
        strong_spawn_interval = csv.strong_spawn_interval;
        reward_per_second = csv.reward_per_second;
        reward_item_id = csv.reward_item_id;
        fan_per_second = csv.fan_per_second;
        prefab = csv.prefab;
        stage_position = csv.stage_position;
    }
}
