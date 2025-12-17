using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HeartStage.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// NewLobby 씬에 애니메이션 시스템 자동 설정
/// Tools > HeartStage > Setup Lobby Animations
/// </summary>
public class LobbyAnimationSetup : EditorWindow
{
    // 클래스 이름 → WindowType 매핑
    private static readonly Dictionary<string, WindowType> windowTypeMap = new Dictionary<string, WindowType>
    {
        // 메인 네비게이션
        { "LobbyHome", WindowType.LobbyHome },
        { "ShoppingWindow", WindowType.Shopping },
        { "GachaUI", WindowType.Gacha },
        { "DormWindow", WindowType.Dorm },
        { "EncyclopediaWindow", WindowType.CharacterDict },
        { "StageSelect", WindowType.StageSelect },
        { "StageWindow", WindowType.StageSelect },
        { "SpecialDungeonUI", WindowType.SpecialDungeon },

        // 서브 윈도우
        { "StageInfoWindow", WindowType.StageInfo },
        { "GachaPercentageUI", WindowType.GachaPercentage },
        { "GachaResultUI", WindowType.GachaResult },
        { "Gacha5TryResultUI", WindowType.Gacha5TryResult },
        { "QuestWindow", WindowType.Quest },
        { "GachaCancelUI", WindowType.GachaCancel },
        { "MonitoringCharacterSelectUI", WindowType.MonitoringCharacterSelect },
        { "MonitoringRewardUI", WindowType.MonitoringReward },
        { "MailUI", WindowType.MailUI },
        { "MailInfoUI", WindowType.MailInfoUI },
        { "SettingPanel", WindowType.SettingPanel },
        { "StoryDungeonUI", WindowType.StoryDungeon },
        { "StoryDungeonInfoUI", WindowType.StoryDungeonInfo },
        { "SpecialStageUI", WindowType.SpecialStage },

        // 인게임 윈도우
        { "VictoryDefeatPanel", WindowType.VictoryDefeat },
        { "CharacterInfoWindow", WindowType.CharacterInfo },
        { "LastStageNoticeUI", WindowType.LastStageNotice },
        { "LosePanelUI", WindowType.LosePanelUI },
        { "VictoryPanelUI", WindowType.VictoryPanelUI },
        { "BossAlertUI", WindowType.BossAlert },
    };

    // 윈도우 타입별 애니메이션 매핑
    private static readonly Dictionary<string, (WindowAnimationTrigger.AnimType open, WindowAnimationTrigger.AnimType close)> animationMap
        = new Dictionary<string, (WindowAnimationTrigger.AnimType, WindowAnimationTrigger.AnimType)>
    {
        // 메인 페이지들 - 페이지 슬라이드
        { "LobbyHome", (WindowAnimationTrigger.AnimType.FadeIn, WindowAnimationTrigger.AnimType.FadeIn) },
        { "ShoppingWindow", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "GachaUI", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "DormWindow", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "EncyclopediaWindow", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "StageSelect", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "StageWindow", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "SpecialDungeonUI", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },
        { "StoryDungeonUI", (WindowAnimationTrigger.AnimType.PageSlideFromRight, WindowAnimationTrigger.AnimType.PageSlideFromRight) },

        // 오버레이/모달 - 스케일 인
        { "QuestWindow", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "MailUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "SettingPanel", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "ProfileWindow", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "FriendListWindow", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },

        // 정보창 - 슬라이드 업
        { "StageInfoWindow", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "MailInfoUI", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "StoryDungeonInfoUI", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "CharacterInfoWindow", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "CharacterDetailPanel", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },

        // 결과창 - 스케일 인
        { "GachaResultUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "Gacha5TryResultUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "GachaPercentageUI", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "GachaCancelUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "VictoryDefeatPanel", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "VictoryPanelUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "LosePanelUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },

        // 특수 UI
        { "BossAlertUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "LastStageNoticeUI", (WindowAnimationTrigger.AnimType.SlideDown, WindowAnimationTrigger.AnimType.SlideDown) },
        { "SpecialStageUI", (WindowAnimationTrigger.AnimType.SlideUp, WindowAnimationTrigger.AnimType.SlideUp) },
        { "MonitoringCharacterSelectUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
        { "MonitoringRewardUI", (WindowAnimationTrigger.AnimType.ScaleIn, WindowAnimationTrigger.AnimType.ScaleIn) },
    };

    [MenuItem("Tools/HeartStage/Setup Lobby Animations", false, 101)]
    public static void SetupAnimations()
    {
        // 1. LobbyAnimations 싱글톤 생성 (없으면)
        LobbyAnimations lobbyAnim = FindFirstObjectByType<LobbyAnimations>();
        if (lobbyAnim == null)
        {
            GameObject animGO = new GameObject("LobbyAnimations");
            lobbyAnim = animGO.AddComponent<LobbyAnimations>();
            Debug.Log("[Setup] LobbyAnimations 오브젝트 생성됨");
        }

        // 2. 모든 GenericWindow에 WindowAnimationTrigger 추가 + 자동 설정
        GenericWindow[] windows = FindObjectsByType<GenericWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int addedCount = 0;
        int configuredCount = 0;

        foreach (var window in windows)
        {
            var trigger = window.GetComponent<WindowAnimationTrigger>();

            // 트리거 없으면 추가
            if (trigger == null)
            {
                trigger = window.gameObject.AddComponent<WindowAnimationTrigger>();
                addedCount++;
            }

            // 윈도우 이름으로 애니메이션 자동 설정
            string windowName = window.GetType().Name;

            if (animationMap.TryGetValue(windowName, out var animPair))
            {
                // SerializedObject로 private 필드 설정
                SerializedObject so = new SerializedObject(trigger);
                so.FindProperty("openAnimation").enumValueIndex = (int)animPair.open;
                so.FindProperty("closeAnimation").enumValueIndex = (int)animPair.close;
                so.ApplyModifiedProperties();

                configuredCount++;
                Debug.Log($"[Setup] {windowName}: {animPair.open} / {animPair.close}");
            }
            else
            {
                // 기본값: ScaleIn
                SerializedObject so = new SerializedObject(trigger);
                so.FindProperty("openAnimation").enumValueIndex = (int)WindowAnimationTrigger.AnimType.ScaleIn;
                so.FindProperty("closeAnimation").enumValueIndex = (int)WindowAnimationTrigger.AnimType.ScaleIn;
                so.ApplyModifiedProperties();

                Debug.Log($"[Setup] {windowName}: 기본값 ScaleIn 적용");
            }
        }

        // 3. 씬 저장
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<color=green>[Setup] 완료! {windows.Length}개 윈도우, {addedCount}개 트리거 추가, {configuredCount}개 자동 설정</color>");

        EditorUtility.DisplayDialog("Setup Complete",
            $"애니메이션 자동 설정 완료!\n\n" +
            $"- 윈도우 발견: {windows.Length}개\n" +
            $"- 트리거 추가: {addedCount}개\n" +
            $"- 자동 설정: {configuredCount}개\n\n" +
            "Ctrl+S로 씬 저장하세요.",
            "OK");
    }

    [MenuItem("Tools/HeartStage/Remove All Animation Triggers", false, 102)]
    public static void RemoveAllTriggers()
    {
        WindowAnimationTrigger[] triggers = FindObjectsByType<WindowAnimationTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = triggers.Length;

        foreach (var trigger in triggers)
        {
            DestroyImmediate(trigger);
        }

        LobbyAnimations lobbyAnimRemove = FindFirstObjectByType<LobbyAnimations>();
        if (lobbyAnimRemove != null)
        {
            DestroyImmediate(lobbyAnimRemove.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Setup] {count}개 트리거 제거됨");
    }

    [MenuItem("Tools/HeartStage/Setup WindowManager", false, 103)]
    public static void SetupWindowManager()
    {
        // WindowManager 찾기
        WindowManager windowManager = FindFirstObjectByType<WindowManager>();
        if (windowManager == null)
        {
            EditorUtility.DisplayDialog("Error", "WindowManager를 찾을 수 없습니다!", "OK");
            return;
        }

        // 씬의 모든 GenericWindow 찾기
        GenericWindow[] allWindows = FindObjectsByType<GenericWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // WindowType → GenericWindow 매핑 생성
        Dictionary<WindowType, GenericWindow> foundWindows = new Dictionary<WindowType, GenericWindow>();
        List<string> unmappedWindows = new List<string>();

        foreach (var window in allWindows)
        {
            string className = window.GetType().Name;

            if (windowTypeMap.TryGetValue(className, out WindowType windowType))
            {
                if (!foundWindows.ContainsKey(windowType))
                {
                    foundWindows[windowType] = window;
                    Debug.Log($"[Setup] {className} → {windowType}");
                }
            }
            else
            {
                unmappedWindows.Add(className);
            }
        }

        // WindowManager의 windowList 설정
        SerializedObject so = new SerializedObject(windowManager);
        SerializedProperty listProp = so.FindProperty("windowList");

        // 기존 리스트 클리어
        listProp.ClearArray();

        // 새로운 WindowPair 추가 (WindowType 순서대로)
        var sortedWindows = foundWindows.OrderBy(x => (int)x.Key).ToList();
        foreach (var kvp in sortedWindows)
        {
            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            SerializedProperty element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            element.FindPropertyRelative("windowType").enumValueIndex = GetEnumIndex(kvp.Key);
            element.FindPropertyRelative("window").objectReferenceValue = kvp.Value;
        }

        so.ApplyModifiedProperties();

        // 씬 저장
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // 결과 출력
        string message = $"WindowManager 설정 완료!\n\n" +
            $"- 매핑된 윈도우: {foundWindows.Count}개\n";

        if (unmappedWindows.Count > 0)
        {
            message += $"\n⚠️ 매핑 안됨 ({unmappedWindows.Count}개):\n";
            foreach (var name in unmappedWindows)
            {
                message += $"  - {name}\n";
            }
        }

        message += "\nCtrl+S로 씬 저장하세요.";

        Debug.Log($"<color=green>[Setup] WindowManager 설정 완료! {foundWindows.Count}개 윈도우 매핑됨</color>");
        EditorUtility.DisplayDialog("Setup Complete", message, "OK");
    }

    // WindowType enum의 실제 인덱스 반환 (enum 값이 연속적이지 않을 수 있으므로)
    private static int GetEnumIndex(WindowType windowType)
    {
        var values = System.Enum.GetValues(typeof(WindowType));
        for (int i = 0; i < values.Length; i++)
        {
            if ((WindowType)values.GetValue(i) == windowType)
                return i;
        }
        return 0;
    }

    [MenuItem("Tools/HeartStage/Reset All (Clear WindowManager)", false, 110)]
    public static void ResetAll()
    {
        // 1. 모든 WindowAnimationTrigger 제거
        WindowAnimationTrigger[] triggers = FindObjectsByType<WindowAnimationTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var trigger in triggers)
        {
            DestroyImmediate(trigger);
        }

        // 2. LobbyAnimations 제거
        LobbyAnimations lobbyAnim = FindFirstObjectByType<LobbyAnimations>();
        if (lobbyAnim != null)
        {
            DestroyImmediate(lobbyAnim.gameObject);
        }

        // 3. WindowManager의 windowList 클리어
        WindowManager windowManager = FindFirstObjectByType<WindowManager>();
        if (windowManager != null)
        {
            SerializedObject so = new SerializedObject(windowManager);
            SerializedProperty listProp = so.FindProperty("windowList");
            listProp.ClearArray();
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=yellow>[Reset] 모든 애니메이션 설정 초기화됨</color>");
        EditorUtility.DisplayDialog("Reset Complete",
            "모든 설정이 초기화되었습니다.\n\n" +
            "- WindowAnimationTrigger 제거됨\n" +
            "- LobbyAnimations 제거됨\n" +
            "- WindowManager.windowList 클리어됨\n\n" +
            "Ctrl+S로 씬 저장 후 직접 설정하세요.",
            "OK");
    }

    [MenuItem("Tools/HeartStage/Validate WindowManager", false, 104)]
    public static void ValidateWindowManager()
    {
        WindowManager windowManager = FindFirstObjectByType<WindowManager>();
        if (windowManager == null)
        {
            EditorUtility.DisplayDialog("Error", "WindowManager를 찾을 수 없습니다!", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(windowManager);
        SerializedProperty listProp = so.FindProperty("windowList");

        List<string> issues = new List<string>();
        List<string> configured = new List<string>();

        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(i);
            var windowTypeIndex = element.FindPropertyRelative("windowType").enumValueIndex;
            var windowRef = element.FindPropertyRelative("window").objectReferenceValue;

            var allTypes = System.Enum.GetValues(typeof(WindowType));
            WindowType windowType = (WindowType)allTypes.GetValue(windowTypeIndex);

            if (windowRef == null)
            {
                issues.Add($"❌ {windowType}: 윈도우 참조 없음");
            }
            else
            {
                configured.Add($"✓ {windowType}: {windowRef.name}");
            }
        }

        string message = $"WindowManager 검증 결과\n\n";
        message += $"설정된 윈도우: {configured.Count}개\n";

        if (issues.Count > 0)
        {
            message += $"\n⚠️ 문제 ({issues.Count}개):\n";
            foreach (var issue in issues)
            {
                message += $"  {issue}\n";
            }
        }
        else
        {
            message += "\n✅ 모든 설정 정상!";
        }

        EditorUtility.DisplayDialog("Validation Result", message, "OK");
    }
}
