using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class TutorialScriptCSVData
{
    public int location { get; set; }
    public int stage_ID { get; set; }
    public int Line { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public string CutImage { get; set; }
    public string Voice { get; set; }
}

public class TutorialScriptTable : DataTable
{
    // 로케이션별로 대사 리스트를 관리
    private readonly Dictionary<int, List<TutorialScriptCSVData>> scriptsByLocation = new Dictionary<int, List<TutorialScriptCSVData>>();

    // 전체 스크립트 리스트 (순서 유지)
    private readonly List<TutorialScriptCSVData> allScripts = new List<TutorialScriptCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        scriptsByLocation.Clear();
        allScripts.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<TutorialScriptCSVData>(ta.text);

        foreach (var item in list)
        {
            // 빈 데이터는 스킵 (Text가 비어있는 경우)
            if (string.IsNullOrEmpty(item.Text))
                continue;

            // 전체 리스트에 추가
            allScripts.Add(item);

            // 로케이션별 딕셔너리에 추가
            if (!scriptsByLocation.ContainsKey(item.location))
            {
                scriptsByLocation[item.location] = new List<TutorialScriptCSVData>();
            }
            scriptsByLocation[item.location].Add(item);
        }

        // 각 로케이션별로 Line 순서대로 정렬
        foreach (var locationScripts in scriptsByLocation.Values)
        {
            locationScripts.Sort((a, b) => a.Line.CompareTo(b.Line));
        }

        Addressables.Release(handle);

        Debug.Log($"튜토리얼 스크립트 로드 완료: {scriptsByLocation.Count}개 로케이션, 총 {allScripts.Count}개 대사");
    }

    /// 특정 로케이션의 모든 대사 가져오기
    public List<TutorialScriptCSVData> GetLocationScripts(int locationId)
    {
        if (!scriptsByLocation.ContainsKey(locationId))
        {
            Debug.LogWarning($"로케이션 {locationId}의 스크립트를 찾을 수 없습니다.");
            return new List<TutorialScriptCSVData>();
        }
        return new List<TutorialScriptCSVData>(scriptsByLocation[locationId]);
    }

    /// 특정 로케이션의 특정 라인 대사 가져오기
    public TutorialScriptCSVData GetScriptByLine(int locationId, int line)
    {
        var locationScripts = GetLocationScripts(locationId);
        return locationScripts.FirstOrDefault(script => script.Line == line);
    }

    /// 모든 로케이션 ID 목록 반환
    public List<int> GetAllLocationIds()
    {
        return scriptsByLocation.Keys.OrderBy(id => id).ToList();
    }

    /// 전체 스크립트 데이터 반환 
    public Dictionary<int, List<TutorialScriptCSVData>> GetAllScripts()
    {
        return new Dictionary<int, List<TutorialScriptCSVData>>(scriptsByLocation);
    }

    public bool HasScripts(int locationId)
    {
        return scriptsByLocation.ContainsKey(locationId) && scriptsByLocation[locationId].Count > 0;
    }
}