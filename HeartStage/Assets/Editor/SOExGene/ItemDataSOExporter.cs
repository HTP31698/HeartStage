using UnityEditor;
using UnityEngine;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Collections.Generic;

public class ItemDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/ItemTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/ItemData/"; // SO 경로

    [MenuItem("Tools/Export ItemData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<ItemDataSOExporter>();
        window.titleContent = new GUIContent("ItemData SO Exporter");
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
        // SO 폴더에서 모든 ItemData 검색
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { soFolderPath });
        List<ItemCSVData> dataList = new List<ItemCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData so = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        // CSV로 덮어쓰기
        using (var writer = new StreamWriter(csvFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {dataList.Count}개의 ItemData SO 데이터를 {csvFilePath}에 덮어쓰기 완료!");
    }
}
