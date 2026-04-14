using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public class CharacterCSVData
{
    public int char_id { get; set; }
    public string char_name { get; set; }
    public int char_lv { get; set; }
    public int char_rank { get; set; }
    public int char_type { get; set; }

    public int atk_dmg { get; set; }
    public float atk_speed { get; set; }
    public float atk_range { get; set; }
    public float atk_addcount { get; set; }

    public int bullet_count { get; set; }
    public float bullet_speed { get; set; }
    public int char_hp { get; set; }

    public float crt_chance { get; set; }
    public float crt_dmg { get; set; }

    public int skill_id1 { get; set; }
    public int skill_id2 { get; set; }
    public int skill_id3 { get; set; }
    public int skill_id4 { get; set; }
    public int skill_id5 { get; set; }
    public int skill_id6 { get; set; }

    public string Info { get; set; }

    public string image_PrefabName { get; set; }
    public string data_AssetName { get; set; }
    public string bullet_PrefabName { get; set; }
    public string projectile_AssetName { get; set; }
    public string hitEffect_AssetName { get; set; }
    public string card_imageName { get; set; }
    public string icon_imageName { get; set; }
}

public class CharacterTable : DataTable
{
    public static readonly string Unknown = "키 없음";

    //id 찾기용
    private readonly Dictionary<int, CharacterCSVData> table = new Dictionary<int, CharacterCSVData>();

    //이름 찾기용
    private Dictionary<string, CharacterCSVData> nametable = new Dictionary<string, CharacterCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
        }

        var list = LoadCSV<CharacterCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.char_id))
            {
                table.Add(item.char_id, item);
            }
            else
            {
                Debug.LogError("캐릭터 아이디 중복!");
            }
        }
        foreach (var item in list)
        {
            if (!nametable.ContainsKey(item.char_name))
            {
                nametable.Add(item.char_name, item);
            }
            else
            {
            }
        }

        Addressables.Release(handle);
    }

    public CharacterCSVData Get(int key)
    {
        if (!table.ContainsKey(key))
        {
            Debug.Log($"[CharacterCSVData] Get {key} 실패");
            return null;
        }
        return table[key];
    }

    public List<int> GetSkillIds(int id)
    {
        var data = table[id];
        var skills = new[] { data.skill_id1, data.skill_id2, data.skill_id3, data.skill_id4, data.skill_id5, data.skill_id6 };

        return skills.Where(s => s != 0).ToList();
    }

    public void BuildDefaultSaveDictionaries(
    IEnumerable<string> starterNames,
    out Dictionary<string, bool> unlockedByName,
    out Dictionary<int, int> expById,
    out List<int> ownedBaseIds
    )
    {
        unlockedByName = new Dictionary<string, bool>();
        expById = new Dictionary<int, int>();
        ownedBaseIds = new List<int>();

        // 1) 캐릭터별 기본 row id 뽑기 (내부에서만 int로 사용)
        var baseIdByName = table.Values
            .GroupBy(r => r.char_name)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.char_rank)
                      .ThenBy(r => r.char_lv)
                      .First().char_id
            );

        var starterSet = starterNames != null
            ? new HashSet<string>(starterNames)
            : new HashSet<string>();

        // 2) unlockedByName(name->bool) 자동 세팅
        foreach (var kv in baseIdByName)
        {
            string name = kv.Key;
            int baseId = kv.Value;

            bool isStarter = starterSet.Contains(name);

            // 도감/보유 체크용
            unlockedByName[name] = isStarter;

            // 스타터면 보유 id + exp(0)까지 같이 세팅
            if (isStarter)
            {
                ownedBaseIds.Add(baseId);
                expById[baseId] = 0;
            }
        }
    }

    public CharacterCSVData GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) 
            return null;
        nametable.TryGetValue(name, out var data);
        return data;
    }

    public Dictionary<int, CharacterData> GetAllCharacterData()
    {
        Dictionary<int, CharacterData> result = new Dictionary<int, CharacterData>();

        foreach (var kvp in table)
        {
            var so = ResourceManager.Instance.Get<CharacterData>(kvp.Value.data_AssetName);
            result.Add(kvp.Key, so);
        }

        return result;
    }

    public string GetIconImageName(int charId)
    {
        if (table.TryGetValue(charId, out var data))
        {
            return data.icon_imageName;
        }
        return Unknown;
    }

    /// <summary>
    /// 런타임 중 SO 변경 시 메모리 내 CSVData를 즉시 갱신.
    /// 기존 객체의 필드를 덮어써서 이미 참조 중인 곳에서도 반영됨.
    /// </summary>
    public void UpdateRuntime(CharacterCSVData csvData)
    {
        if (csvData == null) return;
        if (!table.TryGetValue(csvData.char_id, out var existing)) return;

        existing.char_name = csvData.char_name;
        existing.char_lv = csvData.char_lv;
        existing.char_rank = csvData.char_rank;
        existing.char_type = csvData.char_type;
        existing.atk_dmg = csvData.atk_dmg;
        existing.atk_speed = csvData.atk_speed;
        existing.atk_range = csvData.atk_range;
        existing.atk_addcount = csvData.atk_addcount;
        existing.bullet_count = csvData.bullet_count;
        existing.bullet_speed = csvData.bullet_speed;
        existing.char_hp = csvData.char_hp;
        existing.crt_chance = csvData.crt_chance;
        existing.crt_dmg = csvData.crt_dmg;
        existing.skill_id1 = csvData.skill_id1;
        existing.skill_id2 = csvData.skill_id2;
        existing.skill_id3 = csvData.skill_id3;
        existing.skill_id4 = csvData.skill_id4;
        existing.skill_id5 = csvData.skill_id5;
        existing.skill_id6 = csvData.skill_id6;
        existing.Info = csvData.Info;
        existing.image_PrefabName = csvData.image_PrefabName;
        existing.data_AssetName = csvData.data_AssetName;
        existing.bullet_PrefabName = csvData.bullet_PrefabName;
        existing.projectile_AssetName = csvData.projectile_AssetName;
        existing.hitEffect_AssetName = csvData.hitEffect_AssetName;
        existing.card_imageName = csvData.card_imageName;
        existing.icon_imageName = csvData.icon_imageName;
    }

    public string GetIconImageNameByName(string charName)
    {
        if (string.IsNullOrEmpty(charName))
            return Unknown;

        // char_name이 같은 가장 첫 번째 row 하나 찾기
        var row = table.Values.FirstOrDefault(r => r.char_name == charName);
        if (row != null)
            return row.icon_imageName;

        return Unknown;
    }

    /// <summary>
    /// 모든 CSV 데이터 반환
    /// </summary>
    public IEnumerable<CharacterCSVData> GetAllCSV()
    {
        return table.Values;
    }

    /// <summary>
    /// char_code로 캐릭터 데이터 반환 (레벨1, 랭크1 기준)
    /// char_code는 char_id의 마지막 4자리 (예: 0101, 0502)
    /// </summary>
    public CharacterCSVData GetByCharCode(string charCode)
    {
        if (string.IsNullOrEmpty(charCode)) return null;

        // char_code가 일치하는 첫 번째 row 반환 (lv1, rank1)
        foreach (var data in table.Values)
        {
            string code = (data.char_id % 10000).ToString("D4");
            if (code == charCode && data.char_lv == 1 && data.char_rank == 1)
            {
                return data;
            }
        }
        return null;
    }
}