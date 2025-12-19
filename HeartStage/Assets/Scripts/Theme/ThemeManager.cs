using System;
using UnityEngine;

/// <summary>
/// ThemeManager 싱글톤
/// 현재 테마 제공 + 변경 이벤트
/// </summary>
public class ThemeManager : MonoBehaviour
{
    private static ThemeManager _instance;
    public static ThemeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ThemeManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[ThemeManager]");
                    _instance = go.AddComponent<ThemeManager>();
                    DontDestroyOnLoad(go);
                }
            }
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

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 테마 미리보기용
    /// </summary>
    public static Theme EditorPreviewTheme { get; set; }

    public static Color GetColorInEditor(ThemeColorToken token)
    {
        if (EditorPreviewTheme != null)
            return EditorPreviewTheme.GetColor(token);

        if (_instance != null && _instance._currentTheme != null)
            return _instance._currentTheme.GetColor(token);

        return Color.magenta;
    }
#endif
}
