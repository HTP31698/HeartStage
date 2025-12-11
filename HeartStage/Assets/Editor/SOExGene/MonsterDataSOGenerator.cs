using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class MonsterDataSOGenerator : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/MonsterTable.csv"; // CSV 경로
    private string saveFolderPath = "Assets/ScriptableObject/Monsters/"; // SO 저장 경로

    [MenuItem("Tools/Generate MonsterData SO")]
    private static void ShowWindow()
    {
        var window = GetWindow<MonsterDataSOGenerator>();
        window.titleContent = new GUIContent("MonsterData SO Generator");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV → ScriptableObject Generator (Monster)", EditorStyles.boldLabel);

        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);
        saveFolderPath = EditorGUILayout.TextField("SO Save Folder", saveFolderPath);

        if (GUILayout.Button("Generate SOs"))
        {
            GenerateSOs();
        }
    }

    private void GenerateSOs()
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"CSV 파일이 존재하지 않습니다: {csvFilePath}");
            return;
        }

        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);

        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<MonsterCSVData>().ToList();

            foreach (var record in records)
            {
                // 고유한 파일명으로 mon_id 사용 (원하면 mon_name으로 변경 가능)
                string assetName = $"MonsterData_{record.mon_id}.asset";
                string assetPath = Path.Combine(saveFolderPath, assetName);

                MonsterData so = AssetDatabase.LoadAssetAtPath<MonsterData>(assetPath);
                if (so == null)
                {
                    // 없으면 새로 생성
                    so = ScriptableObject.CreateInstance<MonsterData>();
                    AssetDatabase.CreateAsset(so, assetPath);
                }

                so.UpdateData(record);
                EditorUtility.SetDirty(so);

                // Addressable로 등록 (기존 Stage 레이블 재사용)
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                    entry.address = Path.GetFileNameWithoutExtension(assetPath);
                    entry.SetLabel(AddressableLabel.Stage, true, true);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"총 {records.Count}개의 Monster SO 생성 완료!");
        }
    }
}