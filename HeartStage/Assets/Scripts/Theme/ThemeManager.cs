using System;
using UnityEngine;

/// <summary>
/// ThemeManager 싱글톤
/// 현재 테마 제공 + 변경 이벤트
/// </summary>
public class ThemeManager : MonoBehaviour
{
    private static ThemeManager _instance;
    private static bool _isQuitting;

    public static ThemeManager Instance
    {
        get
        {
            if (_isQuitting)
                return null;

            if (_instance == null)
                _instance = FindFirstObjectByType<ThemeManager>();

            return _instance;
        }
    }

    [SerializeField] private Theme _currentTheme;

    /// <summary>
    /// 현재 테마
    /// </summary>
    public Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnThemeChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// 테마 변경 이벤트 (Themed 컴포넌트들이 구독)
    /// </summary>
    public event Action OnThemeChanged;

    /// <summary>
    /// 토큰으로 색상 가져오기 (편의 메서드)
    /// </summary>
    public Color GetColor(ThemeColorToken token)
    {
        if (_currentTheme == null)
        {
            Debug.LogWarning("[ThemeManager] 현재 테마가 설정되지 않았습니다.");
            return Color.magenta;
        }

        return _currentTheme.GetColor(token);
    }

    /// <summary>
    /// 테마 변경 알림 (수동 호출용)
    /// </summary>
    public void NotifyThemeChanged()
    {
        OnThemeChanged?.Invoke();
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 테마 미리보기용
    /// </summary>
    public static Theme EditorPreviewTheme { get; set; }

    public static Color GetColorInEditor(ThemeColorToken token)
    {
        // EditorPreviewTheme이 null이면 자동 로드 시도
        if (EditorPreviewTheme == null)
        {
            TryAutoLoadEditorTheme();
        }

        if (EditorPreviewTheme != null)
            return EditorPreviewTheme.GetColor(token);

        if (_instance != null && _instance._currentTheme != null)
            return _instance._currentTheme.GetColor(token);

        return Color.magenta;
    }

    /// <summary>
    /// 도메인 리로드 후 EditorPreviewTheme 자동 복원
    /// </summary>
    private static void TryAutoLoadEditorTheme()
    {
        // EditorPrefs에서 마지막 사용 테마 경로 가져오기
        string lastThemePath = UnityEditor.EditorPrefs.GetString("ThemeManager_LastThemePath", "");

        if (!string.IsNullOrEmpty(lastThemePath))
        {
            var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<Theme>(lastThemePath);
            if (theme != null)
            {
                EditorPreviewTheme = theme;
                return;
            }
        }

        // 없으면 첫 번째 테마 자동 로드
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Theme");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            EditorPreviewTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<Theme>(path);

            if (EditorPreviewTheme != null)
            {
                UnityEditor.EditorPrefs.SetString("ThemeManager_LastThemePath", path);
            }
        }
    }

    /// <summary>
    /// EditorPreviewTheme 설정 시 경로 저장
    /// </summary>
    public static void SetEditorPreviewTheme(Theme theme)
    {
        EditorPreviewTheme = theme;
        if (theme != null)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(theme);
            UnityEditor.EditorPrefs.SetString("ThemeManager_LastThemePath", path);
        }
    }

    /// <summary>
    /// 에디터 플레이모드 종료 시 플래그 설정
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void SetupEditorCallbacks()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            _isQuitting = true;
        }
        else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
        {
            _isQuitting = false;
            _instance = null;
        }
    }
#endif
}
