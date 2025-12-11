using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SynergyDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/SynergyTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/SynergyData/"; // SO 경로

    [MenuItem("Tools/Export SynergyData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<SynergyDataSOExporter>();
        window.titleContent = new GUIContent("SynergyData SO Exporter");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("SO → CSV Exporter (SynergyData)", EditorStyles.boldLabel);

        soFolderPath = EditorGUILayout.TextField("SO Folder Path", soFolderPath);
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);

        if (GUILayout.Button("Export to CSV"))
        {
            ExportToCSV();
        }
    }

    private void ExportToCSV()
    {
        string[] guids = AssetDatabase.FindAssets("t:SynergyData", new[] { soFolderPath });
        List<SynergyCSVData> dataList = new List<SynergyCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SynergyData so = AssetDatabase.LoadAssetAtPath<SynergyData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        dataList = dataList
                 .OrderBy(d => d.synergy_id)
                 .ToList();

        if (dataList.Count == 0)
        {
            Debug.LogWarning($"해당 폴더에 SynergyData SO가 없습니다: {soFolderPath}");
            return;
        }

        using (var writer = new StreamWriter(csvFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {dataList.Count}개의 SynergyData SO 데이터를 {csvFilePath}에 저장 완료!");
    }
}