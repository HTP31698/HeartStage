using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TitleTable : DataTable
{
    public static readonly string Unknown = "타이틀 ID 없음";

    private readonly Dictionary<int, TitleData> table = new Dictionary<int, TitleData>();

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

        // CSV를 TitleData 타입으로 파싱하도록 변경
        var list = LoadCSV<TitleData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Title_ID))
            {
                table.Add(item.Title_ID, item);
            }
            else
            {
                Debug.LogError($"타이틀 ID 중복! Title_ID: {item.Title_ID}");
            }
        }

        Addressables.Release(handle);
    }

    public TitleData Get(int titleId)
    {
        if (!table.ContainsKey(titleId))
        {
            return null;
        }

        return table[titleId];
    }

    // CSV에서 로드된 TitleData 전체 반환
    public Dictionary<int, TitleData> GetAll()
    {
        // 필요한 경우 복사본을 반환하도록 변경 가능
        return new Dictionary<int, TitleData>(table);
    }

    // 원래 CSV 데이터 사전 그대로 반환 (참조)
    public Dictionary<int, TitleData> GetAllCSV()
    {
        return table;
    }
}