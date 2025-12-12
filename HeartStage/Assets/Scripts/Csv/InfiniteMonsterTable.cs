using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 무한 스테이지 전용 몬스터 데이터
/// </summary>
public class InfiniteMonsterCSVData
{
    // ========== 기본 정보 ==========
    public int mon_id { get; set; }                 // 몬스터 ID (24xxx)
    public string mon_name { get; set; }            // 몬스터 이름
    public int mon_type { get; set; }               // 몬스터 타입 (1:일반, 2:특수-이속, 3:특수-탱커, 4:특수-공격)

    // ========== 전투 스탯 ==========
    public int atk_type { get; set; }               // 공격 방식 (1:근접, 2:원거리)
    public int atk_dmg { get; set; }                // 기본 공격력
    public float atk_speed { get; set; }            // 공격 속도
    public float atk_range_min { get; set; }        // 최소 공격 사거리
    public float atk_range_max { get; set; }        // 최대 공격 사거리
    public int bullet_speed { get; set; }           // 투사체 속도 (원거리)
    public int hp { get; set; }                     // 기본 체력
    public float speed { get; set; }                // 이동 속도

    // ========== 스킬 ==========
    public int skill_id1 { get; set; }              // 스킬 ID 1
    public int skill_id2 { get; set; }              // 스킬 ID 2
    public int skill_id3 { get; set; }              // 스킬 ID 3

    // ========== 함성 게이지 (경험치) ==========
    public int cheer_min { get; set; }              // 처치 시 함성 게이지 최소
    public int cheer_max { get; set; }              // 처치 시 함성 게이지 최대

    // ========== 드롭 아이템 ==========
    public int item_id1 { get; set; }               // 드롭 아이템 1 ID
    public int drop_count1 { get; set; }            // 드롭 아이템 1 수량
    public int item_id2 { get; set; }               // 드롭 아이템 2 ID
    public int drop_count2 { get; set; }            // 드롭 아이템 2 수량

    // ========== 비주얼 ==========
    public string sprite_pool { get; set; }         // 랜덤 스프라이트 풀 (파이프 구분: "InfMon1|InfMon2|InfMon3")
    public string anim_pool { get; set; }           // 랜덤 애니메이션 풀 (파이프 구분)
}

public class InfiniteMonsterTable : DataTable
{
    private readonly Dictionary<int, InfiniteMonsterCSVData> table = new Dictionary<int, InfiniteMonsterCSVData>();

    // 타입별 캐시
    private readonly List<InfiniteMonsterCSVData> normalMonsters = new List<InfiniteMonsterCSVData>();
    private readonly List<InfiniteMonsterCSVData> fastMonsters = new List<InfiniteMonsterCSVData>();
    private readonly List<InfiniteMonsterCSVData> tankMonsters = new List<InfiniteMonsterCSVData>();
    private readonly List<InfiniteMonsterCSVData> strongMonsters = new List<InfiniteMonsterCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        normalMonsters.Clear();
        fastMonsters.Clear();
        tankMonsters.Clear();
        strongMonsters.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<InfiniteMonsterCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.mon_id))
            {
                table.Add(item.mon_id, item);

                // 타입별 분류
                switch (item.mon_type)
                {
                    case 1: normalMonsters.Add(item); break;
                    case 2: fastMonsters.Add(item); break;
                    case 3: tankMonsters.Add(item); break;
                    case 4: strongMonsters.Add(item); break;
                }
            }
            else
            {
                Debug.LogError($"무한 몬스터 아이디 중복: {item.mon_id}");
            }
        }

        Addressables.Release(handle);
    }

    /// <summary>
    /// 몬스터 데이터 가져오기
    /// </summary>
    public InfiniteMonsterCSVData Get(int monsterId)
    {
        if (!table.ContainsKey(monsterId))
        {
            Debug.LogWarning($"무한 몬스터 아이디를 찾을 수 없음: {monsterId}");
            return null;
        }
        return table[monsterId];
    }

    /// <summary>
    /// 모든 몬스터 데이터
    /// </summary>
    public IEnumerable<InfiniteMonsterCSVData> GetAll()
    {
        return table.Values;
    }

    /// <summary>
    /// 일반 몬스터 목록
    /// </summary>
    public List<InfiniteMonsterCSVData> GetNormalMonsters()
    {
        return normalMonsters;
    }

    /// <summary>
    /// 이속형 특수 몬스터 목록
    /// </summary>
    public List<InfiniteMonsterCSVData> GetFastMonsters()
    {
        return fastMonsters;
    }

    /// <summary>
    /// 탱커형 특수 몬스터 목록
    /// </summary>
    public List<InfiniteMonsterCSVData> GetTankMonsters()
    {
        return tankMonsters;
    }

    /// <summary>
    /// 공격형 특수 몬스터 목록
    /// </summary>
    public List<InfiniteMonsterCSVData> GetStrongMonsters()
    {
        return strongMonsters;
    }

    /// <summary>
    /// 스프라이트 풀 파싱 (파이프 구분: "InfMon1|InfMon2|InfMon3")
    /// </summary>
    public List<string> ParseSpritePool(InfiniteMonsterCSVData data)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(data.sprite_pool))
            return result;

        var names = data.sprite_pool.Split('|');
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }
        return result;
    }

    /// <summary>
    /// 애니메이션 풀 파싱 (파이프 구분)
    /// </summary>
    public List<string> ParseAnimPool(InfiniteMonsterCSVData data)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(data.anim_pool))
            return result;

        var names = data.anim_pool.Split('|');
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }
        return result;
    }

    /// <summary>
    /// 드롭 아이템 정보 가져오기
    /// </summary>
    public Dictionary<int, int> GetDropItemInfo(int monsterId)
    {
        var dict = new Dictionary<int, int>();
        if (!table.ContainsKey(monsterId))
            return dict;

        var data = table[monsterId];
        if (data.item_id1 != 0)
            dict[data.item_id1] = data.drop_count1;
        if (data.item_id2 != 0)
            dict[data.item_id2] = data.drop_count2;

        return dict;
    }
}
