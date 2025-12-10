using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class QuestTable : DataTable
{
    public static readonly string Unknown = "퀘스트 ID 없음";

    private readonly Dictionary<int, QuestData> table = new Dictionary<int, QuestData>();

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

        var list = LoadCSV<QuestData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Quest_ID))
            {
                table.Add(item.Quest_ID, item);
            }
        }

        Addressables.Release(handle);
    }

    public QuestData Get(int questId)
    {
        if (!table.ContainsKey(questId))
        {
            return null;
        }

        return table[questId];
    }

    public bool TryGetNext(int currentId, out QuestData next)
    {
        next = null;
        var cur = Get(currentId);
        if (cur == null)
            return false;
        return table.TryGetValue(cur.Quest_ID, out next);
    }

    public IEnumerable<QuestData> GetByType(QuestType type)
    {
        foreach (var kvp in table)
        {
            var q = kvp.Value;
            if (q.Quest_type == type)
                yield return q;
        }
    }

    /// <summary>
    /// 이벤트 타입에 해당하는 퀘스트 목록 반환
    /// </summary>
    public IEnumerable<QuestData> GetByEventType(QuestEventType eventType)
    {
        foreach (var kvp in table)
        {
            var q = kvp.Value;
            if (q.Event_type == eventType)
                yield return q;
        }
    }

    /// <summary>
    /// 이벤트 타입 + Target ID 매칭 퀘스트 목록 반환
    /// targetId=0이면 해당 이벤트 타입의 전체 대상 퀘스트만 반환
    /// targetId>0이면 Target_ID가 0(전체) 또는 targetId와 일치하는 퀘스트 반환
    /// </summary>
    public IEnumerable<QuestData> GetByEventTypeAndTarget(QuestEventType eventType, int targetId)
    {
        foreach (var kvp in table)
        {
            var q = kvp.Value;
            if (q.Event_type != eventType)
                continue;

            // targetId가 0이면 전체 대상 퀘스트만
            // targetId > 0이면 Target_ID가 0(전체) 또는 매칭되는 퀘스트
            if (targetId == 0)
            {
                if (q.Target_ID == 0)
                    yield return q;
            }
            else
            {
                if (q.Target_ID == 0 || q.Target_ID == targetId)
                    yield return q;
            }
        }
    }
}
