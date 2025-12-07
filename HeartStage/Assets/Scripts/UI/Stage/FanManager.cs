using Unity.Properties;
using UnityEngine;
using System.Collections.Generic;

public class FanManager : MonoBehaviour
{
    [SerializeField] private int finalWaveFanCount = 12; 
    private float moveSpeed = 3f;
    private float fanSpacing = 1f;

    private readonly string[] fanPrefabName =
    {
        "Fan_01", "Fan_02", "Fan_03", "Fan_04", "Fan_05", "Fan_06",
        "Fan_07", "Fan_08", "Fan_09", "Fan_10", "Fan_11", "Fan_12"
    };

    private List<GameObject> activeFans = new List<GameObject>();
    private int totalWaveCount = 0; // 총 웨이브 수
    private int currentWaveCount = 0; // 현재까지 클리어한 웨이브 수

    private void OnEnable()
    {
        MonsterSpawner.OnWaveCleared += SpawanFansWaveClear;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        MonsterSpawner.OnWaveCleared -= SpawanFansWaveClear;
    }

    private void Start()
    {
        // 현재 스테이지의 총 웨이브 수 계산
        CalculateTotalWaveCount();
    }

    private void CalculateTotalWaveCount()
    {
        if (StageManager.Instance == null) return;

        var currentStageData = StageManager.Instance.GetCurrentStageData();
        if (currentStageData == null) return;

        // 현재 스테이지의 모든 웨이브 ID 가져오기
        var stageWaveIds = DataTableManager.StageTable.GetWaveIds(currentStageData.stage_ID);
        totalWaveCount = stageWaveIds.Count;
    }

    private void SpawanFansWaveClear()
    {
        currentWaveCount++; // 웨이브 클리어 시 카운트 증가

        // 마지막 웨이브인지 확인
        bool isFinalWave = currentWaveCount >= totalWaveCount;

        // 마지막 웨이브일 때만 팬 스폰
        if (isFinalWave)
        {
            for (int i = 0; i < finalWaveFanCount; i++)
            {
                SpawnFans(i % 4);
            }
        }
    }

    private void SpawnFans(int fanIndex)
    {
        int randomFanIndex = Random.Range(0, fanPrefabName.Length);
        string selectedFanName = fanPrefabName[randomFanIndex];

        var fanPrefab = ResourceManager.Instance.Get<GameObject>(selectedFanName);

        if (fanPrefab == null)
        {
            return;
        }

        GameObject fan = Instantiate(fanPrefab, transform);

        FanBehavior fanBehavior = fan.GetComponent<FanBehavior>();
        if (fanBehavior == null)
        {
            fanBehavior = fan.AddComponent<FanBehavior>();
        }

        //  위치 계산 
        int currentTotalFans = activeFans.Count;
        fanBehavior.SetupFan(fanIndex, moveSpeed, fanSpacing, currentTotalFans);

        activeFans.Add(fan);
    }

    public void ClearAllFans()
    {
        foreach (var fan in activeFans)
        {
            if (fan != null)
            {
                Destroy(fan);
            }
        }
        activeFans.Clear();

        // 팬 클리어 시 웨이브 카운트 초기화
        currentWaveCount = 0;
    }
}