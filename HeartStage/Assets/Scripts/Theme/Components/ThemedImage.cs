using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image.color를 테마 토큰으로 적용
/// Solid: 테마 색상으로 완전 대체
/// Tint: 원본 색상에 테마 색조를 블렌딩 (장식용)
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class ThemedImage : MonoBehaviour
{
    public enum ApplyMode
    {
        Solid,  // 테마 색상으로 완전 대체 (기본)
        Tint    // 원본 색상 + 테마 색조 블렌딩 (장식용)
    }

    [SerializeField] private ApplyMode _applyMode = ApplyMode.Solid;
    [SerializeField] private ThemeColorToken _colorToken = ThemeColorToken.Surface;
    [SerializeField] private bool _preserveAlpha = false;

    [Header("Tint Mode Settings")]
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.15f;
    [SerializeField] private Color _originalColor = Color.white;
    [SerializeField] private bool _originalColorCaptured = false;

    private Image _image;

    public ApplyMode Mode
    {
        get => _applyMode;
        set
        {
            _applyMode = value;
            ApplyTheme();
        }
    }

    public ThemeColorToken ColorToken
    {
        get => _colorToken;
        set
        {
            _colorToken = value;
            ApplyTheme();
        }
    }

    public float TintStrength
    {
        get => _tintStrength;
        set
        {
            _tintStrength = Mathf.Clamp01(value);
            ApplyTheme();
        }
    }

    private void Awake()
    {
        _image = GetComponent<Image>();
        CaptureOriginalColorIfNeeded();
    }

    private void OnEnable()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        CaptureOriginalColorIfNeeded();

        if (ThemeManager.Instance != null)
            ThemeManager.Instance.OnThemeChanged += ApplyTheme;

        ApplyTheme();
    }

    private void OnDisable()
    {
        if (ThemeManager.Instance != null)
            ThemeManager.Instance.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Tint 모드용 원본 색상 캡처
    /// </summary>
    private void CaptureOriginalColorIfNeeded()
    {
        if (_applyMode == ApplyMode.Tint && !_originalColorCaptured && _image != null)
        {
            _originalColor = _image.color;
            _originalColorCaptured = true;
        }
    }

    /// <summary>
    /// 원본 색상 수동 캡처 (에디터에서 사용)
    /// </summary>
    public void CaptureOriginalColor()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        if (_image != null)
        {
            _originalColor = _image.color;
            _originalColorCaptured = true;
        }
    }

    /// <summary>
    /// 원본 색상 리셋
    /// </summary>
    public void ResetOriginalColor()
    {
        _originalColorCaptured = false;
        if (_image != null)
        {
            _originalColor = _image.color;
        }
    }

    public void ApplyTheme()
    {
        if (_image == null) return;

        Color themeColor;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            themeColor = ThemeManager.GetColorInEditor(_colorToken);
        }
        else
#endif
        {
            if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
                return;

            themeColor = ThemeManager.Instance.GetColor(_colorToken);
        }

        Color resultColor;

        if (_applyMode == ApplyMode.Tint)
        {
            // Tint 모드: 원본 색상에 테마 색조를 블렌딩
            // result = Lerp(original, theme, strength)
            resultColor = Color.Lerp(_originalColor, themeColor, _tintStrength);
            resultColor.a = _originalColor.a; // Tint 모드는 항상 원본 알파 유지
        }
        else
        {
            // Solid 모드: 테마 색상으로 완전 대체
            resultColor = themeColor;

            if (_preserveAlpha)
            {
                resultColor.a = _image.color.a;
            }
        }

        _image.color = resultColor;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        // Tint 모드로 변경될 때 원본 색상 캡처
        if (_applyMode == ApplyMode.Tint && !_originalColorCaptured && _image != null)
        {
            // 에디터에서 직접 캡처하면 현재 적용된 색상이 캡처됨
            // 원본 색상을 유지하려면 CaptureOriginalColor() 수동 호출 필요
        }

        ApplyTheme();
    }
#endif
}
