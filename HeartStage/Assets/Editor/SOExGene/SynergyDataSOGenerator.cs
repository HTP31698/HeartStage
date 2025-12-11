using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class SynergyDataSOGenerator : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/SynergyTable.csv"; // CSV 경로
    private string saveFolderPath = "Assets/ScriptableObject/SynergyData/"; // SO 저장 경로

    [MenuItem("Tools/Generate SynergyData SO")]
    private static void ShowWindow()
    {
        var window = GetWindow<SynergyDataSOGenerator>();
        window.titleContent = new GUIContent("SynergyData SO Generator");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV → ScriptableObject Generator", EditorStyles.boldLabel);

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
            var records = csv.GetRecords<SynergyCSVData>().ToList();

            foreach (var record in records)
            {
                string assetName = $"{record.synergy_name}.asset";
                string assetPath = Path.Combine(saveFolderPath, assetName);

                SynergyData so = AssetDatabase.LoadAssetAtPath<SynergyData>(assetPath);
                if (so == null)
                {
                    // 없으면 새로 생성
                    so = ScriptableObject.CreateInstance<SynergyData>();
                    AssetDatabase.CreateAsset(so, assetPath);
                }

                so.UpdateData(record);
                EditorUtility.SetDirty(so);

                // Addressable로 등록
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                    entry.address = Path.GetFileNameWithoutExtension(assetPath);
                    entry.SetLabel(AddressableLabel.Stage, true, true);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"총 {csvFilePath} 라인 수만큼 SO 생성 완료!");
    }
}
