using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StageDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/StageTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/StageData/"; // SO 경로

    [MenuItem("Tools/Export StageData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<StageDataSOExporter>();
        window.titleContent = new GUIContent("StageData SO Exporter");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("SO → CSV Exporter (StageData)", EditorStyles.boldLabel);

        soFolderPath = EditorGUILayout.TextField("SO Folder Path", soFolderPath);
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);

        if (GUILayout.Button("Export to CSV"))
        {
            ExportToCSV();
        }
    }

    private void ExportToCSV()
    {
        string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { soFolderPath });
        List<StageCSVData> dataList = new List<StageCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData so = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        dataList = dataList
                 .OrderBy(d => d.stage_ID)
                 .ToList();

        if (dataList.Count == 0)
        {
            Debug.LogWarning($"해당 폴더에 StageData SO가 없습니다: {soFolderPath}");
            return;
        }

        using (var writer = new StreamWriter(csvFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {dataList.Count}개의 StageData SO 데이터를 {csvFilePath}에 저장 완료!");
    }
}