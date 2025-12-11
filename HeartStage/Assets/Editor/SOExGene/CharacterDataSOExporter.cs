using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CharacterDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/CharacterTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/CharacterData/"; // SO 경로

    [MenuItem("Tools/Export CharacterData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<CharacterDataSOExporter>();
        window.titleContent = new GUIContent("CharacterData SO Exporter");
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
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { soFolderPath });
        List<CharacterCSVData> dataList = new List<CharacterCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData so = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        dataList = dataList
                 .OrderBy(d => d.char_name)
                 .ThenBy(d => d.char_id)
                 .ThenBy(d => d.char_lv)
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
