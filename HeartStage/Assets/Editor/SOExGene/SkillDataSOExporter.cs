using UnityEditor;
using UnityEngine;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Collections.Generic;

public class SkillDataSOExporter : EditorWindow
{
    private string csvFilePath = "Assets/DataTables/SkillTable.csv"; // 덮어쓸 CSV 경로
    private string soFolderPath = "Assets/ScriptableObject/SkillData/"; // SO 경로

    [MenuItem("Tools/Export SkillData SO to CSV")]
    private static void ShowWindow()
    {
        var window = GetWindow<SkillDataSOExporter>();
        window.titleContent = new GUIContent("SkillData SO Exporter");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("SO → CSV Exporter (SkillData)", EditorStyles.boldLabel);

        soFolderPath = EditorGUILayout.TextField("SO Folder Path", soFolderPath);
        csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);

        if (GUILayout.Button("Export to CSV"))
        {
            ExportToCSV();
        }
    }

    private void ExportToCSV()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { soFolderPath });
        List<SkillCSVData> dataList = new List<SkillCSVData>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData so = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (so != null)
            {
                dataList.Add(so.ToCSVData());
            }
        }

        if (dataList.Count == 0)
        {
            Debug.LogWarning($"해당 폴더에 ActiveSkillData SO가 없습니다: {soFolderPath}");
            return;
        }

        using (var writer = new StreamWriter(csvFilePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(dataList);
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {dataList.Count}개의 ActiveSkillData SO 데이터를 {csvFilePath}에 저장 완료!");
    }
}