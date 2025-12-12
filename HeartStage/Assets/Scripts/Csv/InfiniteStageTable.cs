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
    public int stage_id { get; set; }               // 무한 스테이지 ID (예: 90001)
    public string stage_name { get; set; }          // 스테이지 이름

    // ========== 제한 설정 ==========
    public int daily_limit { get; set; }            // 일일 플레이 제한 횟수
    public int deploy_limit { get; set; }           // 배치 인원 제한
    public int max_monsters { get; set; }           // 필드 최대 몬스터 수

    // ========== 스폰/강화 주기 ==========
    public float spawn_interval { get; set; }       // 몬스터 스폰 간격 (초)
    public float enhance_interval { get; set; }     // 스탯 강화 주기 (초)

    // ========== 강화 배율 ==========
    public float atk_mul { get; set; }              // 공격력 강화 배율 (1회당)
    public float hp_mul { get; set; }               // 체력 강화 배율 (1회당)
    public float speed_mul { get; set; }            // 이속 강화 배율 (1회당)

    // ========== 기본 몬스터 풀 ==========
    public string base_mon_ids { get; set; }        // 기본 스폰 몬스터 ID 목록 (콤마 구분: "24001,24002,24003")

    // ========== 특수 몬스터 스폰 ==========
    public int fast_mon_id { get; set; }            // 이속형 몬스터 ID
    public float fast_spawn_time { get; set; }      // 이속형 첫 등장 시간 (초)
    public float fast_spawn_interval { get; set; } // 이속형 반복 스폰 간격 (초, 0이면 1회만)

    public int tank_mon_id { get; set; }            // 탱커형 몬스터 ID
    public float tank_spawn_time { get; set; }      // 탱커형 첫 등장 시간 (초)
    public float tank_spawn_interval { get; set; } // 탱커형 반복 스폰 간격 (초)

    public int strong_mon_id { get; set; }          // 공격형 몬스터 ID
    public float strong_spawn_time { get; set; }    // 공격형 첫 등장 시간 (초)
    public float strong_spawn_interval { get; set; }// 공격형 반복 스폰 간격 (초)

    // ========== 보상 설정 ==========
    public int reward_per_second { get; set; }      // 초당 기본 보상 (라이트스틱 등)
    public int reward_item_id { get; set; }         // 보상 아이템 ID

    // ========== 비주얼 ==========
    public string prefab { get; set; }              // 배경 프리팹명
    public int stage_position { get; set; }         // 스테이지 위치 (1:상, 2:중, 3:하)
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
    /// 기본 몬스터 ID 목록 파싱 (파이프 구분: "24001|24002|24003")
    /// </summary>
    public List<int> ParseBaseMonsterIds(InfiniteStageCSVData data)
    {
        var result = new List<int>();
        if (string.IsNullOrEmpty(data.base_mon_ids))
            return result;

        var ids = data.base_mon_ids.Split('|');
        foreach (var idStr in ids)
        {
            if (int.TryParse(idStr.Trim(), out int id))
            {
                result.Add(id);
            }
        }
        return result;
    }
}
