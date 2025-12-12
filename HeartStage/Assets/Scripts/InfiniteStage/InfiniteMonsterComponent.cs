using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무한 스테이지 몬스터용 컴포넌트
/// - CSV 데이터 저장
/// - 사망 시 InfiniteStageManager에 알림
/// - 드롭 아이템/함성 게이지 처리
/// </summary>
public class InfiniteMonsterComponent : MonoBehaviour
{
    private int monsterId;
    private InfiniteMonsterCSVData csvData;
    private string poolId;

    public int MonsterId => monsterId;
    public InfiniteMonsterCSVData CSVData => csvData;
    public string PoolId => poolId;

    public void SetMonsterId(int id)
    {
        monsterId = id;
    }

    public void SetCSVData(InfiniteMonsterCSVData data)
    {
        csvData = data;
    }

    public void SetPoolId(string id)
    {
        poolId = id;
    }

    /// <summary>
    /// 몬스터 사망 시 호출
    /// </summary>
    public void OnDeath()
    {
        if (InfiniteStageManager.Instance == null)
            return;

        // 함성 게이지 (경험치) 계산
        int cheerValue = 0;
        if (csvData != null)
        {
            cheerValue = Random.Range(csvData.cheer_min, csvData.cheer_max + 1);
        }

        // 드롭 아이템 수집
        Dictionary<int, int> dropItems = GetDropItems();

        // 매니저에 알림
        InfiniteStageManager.Instance.OnMonsterDeath(gameObject, cheerValue, dropItems);
    }

    private Dictionary<int, int> GetDropItems()
    {
        var items = new Dictionary<int, int>();

        if (csvData == null)
            return items;

        if (csvData.item_id1 > 0 && csvData.drop_count1 > 0)
            items[csvData.item_id1] = csvData.drop_count1;

        if (csvData.item_id2 > 0 && csvData.drop_count2 > 0)
            items[csvData.item_id2] = csvData.drop_count2;

        return items;
    }

    private void OnDisable()
    {
        // 비활성화될 때 (풀로 반환될 때) 데이터 초기화하지 않음
        // 재사용 시 SetCSVData로 다시 설정됨
    }
}
