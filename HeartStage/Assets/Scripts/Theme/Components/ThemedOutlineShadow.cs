using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Outline/Shadow의 effectColor를 테마 토큰으로 적용
/// </summary>
[ExecuteAlways]
public class ThemedOutlineShadow : MonoBehaviour
{
    [SerializeField] private ThemeColorToken _colorToken = ThemeColorToken.Border;
    [SerializeField] private bool _preserveAlpha = true;

    private Outline _outline;
    private Shadow _shadow;

    public ThemeColorToken ColorToken
    {
        get => _colorToken;
        set
        {
            _colorToken = value;
            ApplyTheme();
        }
    }

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _shadow = GetComponent<Shadow>();
    }

    private void OnEnable()
    {
        if (_outline == null)
            _outline = GetComponent<Outline>();
        if (_shadow == null)
            _shadow = GetComponent<Shadow>();

        if (ThemeManager.Instance != null)
            ThemeManager.Instance.OnThemeChanged += ApplyTheme;

        ApplyTheme();
    }

    private void OnDisable()
    {
        if (ThemeManager.Instance != null)
            ThemeManager.Instance.OnThemeChanged -= ApplyTheme;
    }

    public void ApplyTheme()
    {
        Color newColor;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            newColor = ThemeManager.GetColorInEditor(_colorToken);
        }
        else
#endif
        {
            if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
                return;

            newColor = ThemeManager.Instance.GetColor(_colorToken);
        }

        // Outline 적용
        if (_outline != null)
        {
            if (_preserveAlpha)
            {
                newColor.a = _outline.effectColor.a;
            }
            _outline.effectColor = newColor;
        }

        // Shadow 적용 (Outline과 별개로)
        if (_shadow != null && _outline == null) // Outline이 있으면 Shadow는 건너뜀 (Outline이 Shadow 상속)
        {
            Color shadowColor = newColor;
            if (_preserveAlpha)
            {
                shadowColor.a = _shadow.effectColor.a;
            }
            _shadow.effectColor = shadowColor;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_outline == null)
            _outline = GetComponent<Outline>();
        if (_shadow == null)
            _shadow = GetComponent<Shadow>();

        ApplyTheme();
    }
#endif
}
