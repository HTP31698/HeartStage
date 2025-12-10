#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 🔥 Ultimate Unity Asset Bookmark Tool
/// ✔ 모든 Asset 지원
/// ✔ Slot 1~3 관리
/// ✔ Ctrl+Shift+D 로 Slot 순환
/// ✔ 자동 저장/로드
/// ✔ Git 미포함 (EditorPrefs)
/// </summary>
public class BookmarkAssetsWindow : EditorWindow
{
    private const int SlotCount = 3;
    private int currentSlot = 0;

    private List<string>[] bookmarkSlots = new List<string>[SlotCount]
    {
        new List<string>(), // Slot 1
        new List<string>(), // Slot 2
        new List<string>()  // Slot 3
    };

    private Vector2 scroll;

    private const string PrefKey = "BookmarkAssetsWindow_Slots";

    // -------------------------------------------
    // ★ 단축키: Ctrl + Shift + D → 다음 Slot 이동
    // -------------------------------------------
    [MenuItem("Tools/Asset Bookmarks/Next Slot %#d")]
    public static void NextSlot()
    {
        var win = GetWindow<BookmarkAssetsWindow>("Asset Bookmarks");

        win.currentSlot = (win.currentSlot + 1) % SlotCount;

        win.Repaint();
        win.Focus();
    }

    [MenuItem("Tools/Asset Bookmarks/Open Window")]
    public static void OpenWindow()
    {
        GetWindow<BookmarkAssetsWindow>("Asset Bookmarks");
    }

    [MenuItem("Tools/Asset Bookmarks/Reset to Default")]
    public static void ResetToDefault()
    {
        if (EditorUtility.DisplayDialog("북마크 초기화",
            "모든 북마크를 기본값으로 초기화합니다.\n계속하시겠습니까?", "확인", "취소"))
        {
            EditorPrefs.DeleteKey(PrefKey);
            var win = GetWindow<BookmarkAssetsWindow>("Asset Bookmarks");
            win.InitializeDefaultBookmarks();
            win.Repaint();
        }
    }

    private void OnEnable()
    {
        LoadBookmarks();
    }

    private void OnDisable()
    {
        SaveBookmarks();
    }

    private void OnGUI()
    {
        DrawTabs();
        DrawSlotUI();
    }

    // -------------------------------------------
    // 상단 Slot 탭 UI
    // -------------------------------------------
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < SlotCount; i++)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontStyle = (i == currentSlot ? FontStyle.Bold : FontStyle.Normal),
                normal = { textColor = (i == currentSlot ? Color.cyan : Color.white) }
            };

            if (GUILayout.Button($"Slot {i + 1}", style, GUILayout.Height(25)))
            {
                currentSlot = i;
                Repaint();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    // -------------------------------------------
    // 슬롯 내부 UI
    // -------------------------------------------
    private void DrawSlotUI()
    {
        var list = bookmarkSlots[currentSlot];

        GUILayout.Box($"★ 드래그하여 북마크 추가 — Slot {currentSlot + 1}",
            GUILayout.Height(40), GUILayout.ExpandWidth(true));

        var rect = GUILayoutUtility.GetLastRect();
        HandleDragAndDrop(rect, list);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            string path = list[i];
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Texture icon = AssetDatabase.GetCachedIcon(path);

            if (GUILayout.Button(new GUIContent($" {System.IO.Path.GetFileName(path)}", icon),
                GUILayout.Height(22)))
            {
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            if (GUILayout.Button("❌", GUILayout.Width(28)))
            {
                list.RemoveAt(i);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // -------------------------------------------
    // 드래그 & 드롭 처리 (모든 Asset 허용)
    // -------------------------------------------
    private void HandleDragAndDrop(Rect rect, List<string> list)
    {
        Event e = Event.current;

        if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) &&
            rect.Contains(e.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (string path in DragAndDrop.paths)
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                    if (obj != null && !list.Contains(path))
                        list.Add(path);
                }
            }

            e.Use();
        }
    }

    // -------------------------------------------
    // ★ 자동 저장 / 자동 로드 (EditorPrefs)
    // -------------------------------------------
    private void SaveBookmarks()
    {
        BookmarkData data = new BookmarkData();

        for (int i = 0; i < SlotCount; i++)
        {
            SlotData slot = new SlotData();
            slot.paths = bookmarkSlots[i].ToList();
            data.slots.Add(slot);
        }

        string json = JsonUtility.ToJson(data, true);
        EditorPrefs.SetString(PrefKey, json);
    }

    private void LoadBookmarks()
    {
        if (!EditorPrefs.HasKey(PrefKey))
        {
            // 첫 실행 시 기본 북마크 설정
            InitializeDefaultBookmarks();
            return;
        }

        string json = EditorPrefs.GetString(PrefKey);
        BookmarkData data = JsonUtility.FromJson<BookmarkData>(json);

        if (data != null && data.slots != null && data.slots.Count > 0)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (i < data.slots.Count && data.slots[i] != null && data.slots[i].paths != null)
                {
                    bookmarkSlots[i] = data.slots[i].paths.ToList();
                }
                else
                {
                    bookmarkSlots[i] = new List<string>();
                }
            }
        }
        else
        {
            // 데이터가 비어있거나 잘못된 경우 기본값 설정
            InitializeDefaultBookmarks();
        }
    }

    private void InitializeDefaultBookmarks()
    {
        // Slot 0: 자주 쓰는 폴더들
        bookmarkSlots[0] = new List<string>
        {
            "Assets/Scripts",
            "Assets/ScriptableObject",
            "Assets/DataTables"
        };

        // Slot 1, 2는 빈 상태로
        bookmarkSlots[1] = new List<string>();
        bookmarkSlots[2] = new List<string>();

        // 바로 저장
        SaveBookmarks();
    }

    [Serializable]
    private class SlotData
    {
        public List<string> paths = new List<string>();
    }

    [Serializable]
    private class BookmarkData
    {
        public List<SlotData> slots = new List<SlotData>();
    }
}

#endif
