#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using CsvHelper;
using System.IO;
using System.Globalization;

public class MonsterDataTools : EditorWindow
{
    private static bool _isProcessing = false;
    private static bool _isInitialized = false;

    [MenuItem("Tools/Monster Data Tools")]
    public static void ShowWindow()
    {
        GetWindow<MonsterDataTools>("Monster Data Tools");
    }

    private void OnGUI()
    {
        GUILayout.Label("Monster Data Tools", EditorStyles.boldLabel);
        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(_isProcessing);

        GUILayout.Label("1. 데이터테이블에서 SO 자동 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("MonsterTable의 모든 데이터를 읽어서 MonsterData SO를 자동 생성합니다.", MessageType.Info);

        if (GUILayout.Button("데이터테이블 → SO 생성", GUILayout.Height(40)))
        {
            CreateSOFromDataTable();
        }

        GUILayout.Space(20);

        GUILayout.Label("2. SO 데이터로 데이터테이블 덮어쓰기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("프로젝트 내 모든 MonsterData SO를 찾아서 데이터테이블을 덮어쓰고 CSV로 저장합니다.", MessageType.Info);

        if (GUILayout.Button("SO → 데이터테이블 덮어쓰기", GUILayout.Height(40)))
        {
            OverwriteDataTableFromSO();
        }

        EditorGUI.EndDisabledGroup();

        if (_isProcessing)
        {
            EditorGUILayout.HelpBox("작업 진행 중...", MessageType.Info);
        }
    }

    private async void CreateSOFromDataTable()
    {
        if (_isProcessing) return;

        _isProcessing = true;
        try
        {
            await EnsureInitialized();

            var monsterTable = DataTableManager.MonsterTable;
            if (monsterTable == null)
             {
                Debug.LogError("MonsterTable이 초기화되지 않았습니다.");
                return;
            }

            string folderPath = "Assets/ScriptableObject/Monsters";
            EnsureFolderExists(folderPath);

            int createCount = 0;
            var allData = monsterTable.GetAllData();

            foreach (var data in allData)
            {
                string assetPath = $"{folderPath}/{data.mon_id}.asset";
                MonsterData existingSO = AssetDatabase.LoadAssetAtPath<MonsterData>(assetPath);

                MonsterData so = existingSO ?? ScriptableObject.CreateInstance<MonsterData>();
                AssignDataToSO(so, data);

                if (existingSO == null)
                {
                    AssetDatabase.CreateAsset(so, assetPath);
                    RegisterToAddressables(assetPath, data.mon_id); // 추가
                    Debug.Log($"새 SO 생성: {data.mon_name} (ID: {data.mon_id})");
                }
                else
                {
                    EditorUtility.SetDirty(so);
                    Debug.Log($"기존 SO 업데이트: {data.mon_name} (ID: {data.mon_id})");
                }

                createCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MonsterData SO 작업 완료! 총 {createCount}개 처리됨");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SO 생성 실패: {e.Message}");
        }
        finally
        {
            _isProcessing = false;
            Repaint();
        }
    }

    private void OverwriteDataTableFromSO()
    {
        if (_isProcessing) return;

        _isProcessing = true;
        try
        {
            string csvPath = "Assets/DataTables/MonsterTable.csv";

            string[] guids = AssetDatabase.FindAssets("t:MonsterData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("MonsterData SO를 찾을 수 없습니다.");
                return;
            }

            List<MonsterCSVData> csvDataList = new List<MonsterCSVData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonsterData so = AssetDatabase.LoadAssetAtPath<MonsterData>(path);

                if (so != null && so.id > 0)
                {
                    csvDataList.Add(so.ToTableData());
                    Debug.Log($"SO 데이터 변환: {so.monsterName} (ID: {so.id})");
                }
            }

            if (csvDataList.Count == 0)
            {
                Debug.LogWarning("변환할 SO 데이터가 없습니다.");
                return;
            }

            csvDataList.Sort((a, b) => a.mon_id.CompareTo(b.mon_id));

            using (var writer = new System.IO.StreamWriter(csvPath))
            using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(csvDataList);
            }

            AssetDatabase.Refresh();
            Debug.Log($"성공! {csvDataList.Count}개 SO 데이터를 {csvPath}에 덮어쓰기 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSV 덮어쓰기 실패: {e.Message}");
        }
        finally
        {
            _isProcessing = false;
            Repaint();
        }
    }

    // 헬퍼 메서드들
    private async UniTask EnsureInitialized()
    {
        if (_isInitialized)
            return;
        
         await DataTableManager.Initialization;
        _isInitialized = true;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObject"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObject");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/ScriptableObject", "Monsters");
    }

    private void AssignDataToSO(MonsterData so, MonsterCSVData data)
    {
        so.id = data.mon_id;
        so.monsterName = data.mon_name;
        so.monsterType = data.mon_type;
        so.hp = data.hp;
        so.att = data.atk_dmg;
        so.attType = data.atk_type;
        so.attackSpeed = data.atk_speed;
        so.attackRange = data.atk_range;
        so.bulletSpeed = data.bullet_speed;
        so.moveSpeed = data.speed;
        so.minExp = data.min_level;
        so.maxExp = data.max_level;

        // 새로운 필드들
        so.skillId1 = data.skill_id1;
        so.skillId2 = data.skill_id2;
        so.skillId3 = data.skill_id3;
        so.itemId1 = data.item_id1;
        so.dropCount1 = data.drop_count1;
        so.itemId2 = data.item_id2;
        so.dropCount2 = data.drop_count2;
        so.prefab1 = data.prefab1;
        so.prefab2 = data.prefab2;
    }

    private void RegisterToAddressables(string assetPath, int monsterId)
    {
#if UNITY_EDITOR
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = $"{monsterId}";
            
            Debug.Log($"Addressables 등록: {monsterId}");
        }
#endif
    }
}
#endif