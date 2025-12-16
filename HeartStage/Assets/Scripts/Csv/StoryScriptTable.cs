using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class StoryScriptCSVData
{
    public int StageID { get; set; }
    public int Line { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public string Voice { get; set; }
}

public class StoryScriptTable : DataTable
{
    // 스테이지 ID별로 대사 리스트를 관리
    private readonly Dictionary<int, List<StoryScriptCSVData>> scriptsByStage = new Dictionary<int, List<StoryScriptCSVData>>();

    // 전체 스크립트 리스트 (순서 유지)
    private readonly List<StoryScriptCSVData> allScripts = new List<StoryScriptCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        scriptsByStage.Clear();
        allScripts.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<StoryScriptCSVData>(ta.text);

        foreach (var item in list)
        {
            // 빈 데이터는 스킵 (StageID가 0이거나 Text가 비어있는 경우)
            if (item.StageID == 0 || string.IsNullOrEmpty(item.Text))
                continue;

            // 전체 리스트에 추가
            allScripts.Add(item);

            // 스테이지별 딕셔너리에 추가
            if (!scriptsByStage.ContainsKey(item.StageID))
            {
                scriptsByStage[item.StageID] = new List<StoryScriptCSVData>();
            }
            scriptsByStage[item.StageID].Add(item);
        }

        // 각 스테이지별로 Line 순서대로 정렬
        foreach (var stageScripts in scriptsByStage.Values)
        {
            stageScripts.Sort((a, b) => a.Line.CompareTo(b.Line));
        }

        Addressables.Release(handle);

        Debug.Log($"스토리 스크립트 로드 완료: {scriptsByStage.Count}개 스테이지, 총 {allScripts.Count}개 대사");
    }

    /// 특정 스테이지의 모든 대사 가져오기
    public List<StoryScriptCSVData> GetStageScripts(int stageId)
    {
        if (!scriptsByStage.ContainsKey(stageId))
        {
            Debug.LogWarning($"스테이지 {stageId}의 스크립트를 찾을 수 없습니다.");
            return new List<StoryScriptCSVData>();
        }
        return new List<StoryScriptCSVData>(scriptsByStage[stageId]);
    }

    /// 특정 스테이지의 특정 라인 대사 가져오기
    public StoryScriptCSVData GetScriptByLine(int stageId, int line)
    {
        var stageScripts = GetStageScripts(stageId);
        return stageScripts.FirstOrDefault(script => script.Line == line);
    }

    /// 모든 스테이지 ID 목록 반환
    public List<int> GetAllStageIds()
    {
        return scriptsByStage.Keys.OrderBy(id => id).ToList();
    }


    /// 전체 스크립트 데이터 반환 
    public Dictionary<int, List<StoryScriptCSVData>> GetAllScripts()
    {
        return new Dictionary<int, List<StoryScriptCSVData>>(scriptsByStage);
    }

    public bool HasScripts(int stageId)
    {
        return scriptsByStage.ContainsKey(stageId) && scriptsByStage[stageId].Count > 0;
    }
}