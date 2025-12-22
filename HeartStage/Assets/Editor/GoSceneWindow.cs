using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoSceneWindow : EditorWindow
{
    private SceneAsset targetScene;

    [MenuItem("Tools/Go Scene")]
    public static void ShowWindow()
    {
        var window = GetWindow<GoSceneWindow>("Go Scene");
        window.minSize = new Vector2(250, 80);
        window.maxSize = new Vector2(400, 80);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Scene", GUILayout.Width(80));
        targetScene = (SceneAsset)EditorGUILayout.ObjectField(
            targetScene,
            typeof(SceneAsset),
            false
        );
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUI.enabled = targetScene != null;
        if (GUILayout.Button("Go!", GUILayout.Height(30)))
        {
            GoToScene();
        }
        GUI.enabled = true;
    }

    private void GoToScene()
    {
        if (targetScene == null) return;

        string scenePath = AssetDatabase.GetAssetPath(targetScene);

        if (EditorApplication.isPlaying)
        {
            // 플레이 모드: 런타임 씬 로드
            SceneManager.LoadScene(targetScene.name);
        }
        else
        {
            // 에디터 모드: 에디터에서 씬 열기
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}
