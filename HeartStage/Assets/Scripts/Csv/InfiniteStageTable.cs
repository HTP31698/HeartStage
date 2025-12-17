using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 무한 스테이지 설정 데이터
/// </summary>
public class InfiniteStageCSVData
{
    // ========== 기본 정보 ==========
    public int stage_id { get; set; }                   // 무한 스테이지 ID
    public string stage_name { get; set; }              // 스테이지 이름

    // ========== 제한 설정 ==========
    public int daily_limit { get; set; }                // 일일 플레이 제한 횟수
    public int deploy_limit { get; set; }               // 배치 가능 캐릭터 수

    // ========== 스폰 설정 ==========
    public int max_monsters { get; set; }               // 필드 몬스터 제한 수량
    public float spawn_interval { get; set; }           // 적군 스폰 간격 (초)

    // ========== 강화 설정 ==========
    public float enhance_interval { get; set; }         // 강화 주기 (초)
    public float atk_mul { get; set; }                  // 공격력 강화 배율 (1회당)
    public float hp_mul { get; set; }                   // 체력 강화 배율 (1회당)
    public float speed_mul { get; set; }                // 이동속도 강화 배율 (1회당)

    // ========== 기본 몬스터 ==========
    public int base_mon_id1 { get; set; }               // 기본 몬스터 1
    public int base_mon_id2 { get; set; }               // 기본 몬스터 2

    // ========== 이속형 특수 몬스터 ==========
    public int fast_mon_id { get; set; }                // 이속형 몬스터 ID
    public int fast_spawn_time { get; set; }            // 이속형 첫 등장 시간 (초)
    public int fast_spawn_interval { get; set; }        // 이속형 반복 스폰 간격 (초)

    // ========== 탱커형 특수 몬스터 ==========
    public int tank_mon_id { get; set; }                // 탱커형 몬스터 ID
    public int tank_spawn_time { get; set; }            // 탱커형 첫 등장 시간 (초)
    public int tank_spawn_interval { get; set; }        // 탱커형 반복 스폰 간격 (초)

    // ========== 강화형 특수 몬스터 ==========
    public int strong_mon_id { get; set; }              // 강화형 몬스터 ID
    public int strong_spawn_time { get; set; }          // 강화형 첫 등장 시간 (초)
    public int strong_spawn_interval { get; set; }      // 강화형 반복 스폰 간격 (초)

    // ========== 보상 설정 ==========
    public int reward_per_second { get; set; }          // 보상 누적 시간 (초)
    public int reward_item_id { get; set; }             // 보상 아이템 ID

    // ========== 기타 ==========
    public string prefab { get; set; }                  // 프리팹 이름
    public int stage_position { get; set; }             // 스테이지 포지션

    // ========== 하위 호환용 프로퍼티 ==========
    public string infinite_stage_name => stage_name;
    public int enemy_filed_count => max_monsters;
    public float enemy_spown_time => spawn_interval;
    public int enforce_time => (int)enhance_interval;
    public float attack_growth_value => atk_mul;
    public float hp_growth_value => hp_mul;
    public float speed_growth_value => speed_mul;
    public int level_max => 100;                        // 기본값
    public int Fever_Time_stack => 3;                   // 기본값
}

public class InfiniteStageTable : DataTable
{
    private readonly Dictionary<int, InfiniteStageCSVData> table = new Dictionary<int, InfiniteStageCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<InfiniteStageCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.stage_id))
            {
                table.Add(item.stage_id, item);
            }
            else
            {
                Debug.LogError($"무한 스테이지 아이디 중복: {item.stage_id}");
            }
        }

        Addressables.Release(handle);
    }

    /// <summary>
    /// 무한 스테이지 데이터 가져오기
    /// </summary>
    public InfiniteStageCSVData Get(int stageId)
    {
        if (!table.ContainsKey(stageId))
        {
            Debug.LogWarning($"무한 스테이지 아이디를 찾을 수 없음: {stageId}");
            return null;
        }
        return table[stageId];
    }

    /// <summary>
    /// 첫 번째 무한 스테이지 데이터 (기본값)
    /// </summary>
    public InfiniteStageCSVData GetFirst()
    {
        foreach (var kvp in table)
        {
            return kvp.Value;
        }
        return null;
    }

    /// <summary>
    /// 모든 무한 스테이지 데이터
    /// </summary>
    public IEnumerable<InfiniteStageCSVData> GetAll()
    {
        return table.Values;
    }

    /// <summary>
    /// 기본 몬스터 ID 목록 반환
    /// </summary>
    public List<int> GetBaseMonsterIds(InfiniteStageCSVData data)
    {
        var result = new List<int>();
        if (data == null)
            return result;

        if (data.base_mon_id1 > 0)
            result.Add(data.base_mon_id1);
        if (data.base_mon_id2 > 0)
            result.Add(data.base_mon_id2);

        return result;
    }
}
