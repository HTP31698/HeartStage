using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StageWaveDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/StageWaveTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/StageWaveData/"; // SO 경로

    [MenuItem("Tools/Export StageWaveData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<StageWaveDataSOExporter>();
        window.titleContent = new GUIContent("StageWaveData SO Exporter");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("SO → CSV Exporter", EditorStyles.boldLabel);

        soFolderPath = EditorGUILayout.TextField("SO Folder Path", soFolderPath);
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);

        if (GUILayout.Button("Export to CSV"))
        {
            ExportToCSV();
        }
    }

    private void ExportToCSV()
    {
        string[] guids = AssetDatabase.FindAssets("t:StageWaveData", new[] { soFolderPath });
        List<StageWaveCSVData> dataList = new List<StageWaveCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageWaveData so = AssetDatabase.LoadAssetAtPath<StageWaveData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        dataList = dataList
                 .OrderBy(d => d.wave_id)
                 .ToList();

        using (var writer = new StreamWriter(csvFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {dataList.Count}개의 SO 데이터를 {csvFilePath}에 덮어쓰기 완료!");
    }
}
