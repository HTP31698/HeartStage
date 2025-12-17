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
}