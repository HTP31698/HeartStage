using UnityEngine;
using TMPro;

/// <summary>
/// TMP_Text.color를 테마 토큰으로 적용
/// + Outline/Face 색상 옵션
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class ThemedTMPText : MonoBehaviour
{
    [Header("텍스트 색상")]
    [SerializeField] private ThemeColorToken _colorToken = ThemeColorToken.TextPrimary;
    [SerializeField] private bool _preserveAlpha = false;

    [Header("Outline (선택)")]
    [SerializeField] private bool _applyOutlineColor = false;
    [SerializeField] private ThemeColorToken _outlineColorToken = ThemeColorToken.TextPrimary;
    [SerializeField] private bool _preserveOutlineAlpha = true;

    [Header("Underlay (선택)")]
    [SerializeField] private bool _applyUnderlayColor = false;
    [SerializeField] private ThemeColorToken _underlayColorToken = ThemeColorToken.TextSecondary;

    private TMP_Text _text;

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
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();

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
        if (_text == null) return;

        Color textColor;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            textColor = ThemeManager.GetColorInEditor(_colorToken);
        }
        else
#endif
        {
            if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
                return;

            textColor = ThemeManager.Instance.GetColor(_colorToken);
        }

        // 텍스트 색상 적용
        if (_preserveAlpha)
        {
            textColor.a = _text.color.a;
        }
        _text.color = textColor;

        // Outline 색상 적용 (Material Property)
        if (_applyOutlineColor && _text.fontMaterial != null)
        {
            Color outlineColor;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                outlineColor = ThemeManager.GetColorInEditor(_outlineColorToken);
            else
#endif
                outlineColor = ThemeManager.Instance.GetColor(_outlineColorToken);

            if (_preserveOutlineAlpha)
            {
                // 기존 아웃라인 알파 유지
                if (_text.fontMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
                {
                    Color currentOutline = _text.fontMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
                    outlineColor.a = currentOutline.a;
                }
            }

            _text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        }

        // Underlay 색상 적용
        if (_applyUnderlayColor && _text.fontMaterial != null)
        {
            Color underlayColor;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                underlayColor = ThemeManager.GetColorInEditor(_underlayColorToken);
            else
#endif
                underlayColor = ThemeManager.Instance.GetColor(_underlayColorToken);

            if (_text.fontMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                _text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, underlayColor);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();

        ApplyTheme();
    }
#endif
}
