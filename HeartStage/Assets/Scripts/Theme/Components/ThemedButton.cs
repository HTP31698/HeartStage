using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button의 ColorBlock + TargetGraphic을 테마 토큰으로 적용
///
/// ===== 적용 방식 =====
/// 1. ColorBlock 방식 (_useColorBlock = true)
///    - Button.colors의 normalColor/highlightedColor/pressedColor/disabledColor 설정
///    - Unity의 기본 상태 전환 애니메이션 활용
///    - Transition Mode가 ColorTint일 때 권장
///
/// 2. TargetGraphic Tint 방식 (_useTint = true)
///    - Button.targetGraphic (Image)의 color 직접 설정
///    - interactable 상태에 따라 normalColor/disabledColor 적용
///    - ColorBlock과 독립적으로 동작 가능
///
/// ===== 두 방식 동시 사용 =====
/// - 기본값: 둘 다 true
/// - ColorBlock: 상태 전환 색상 (마우스 오버, 클릭 등)
/// - Tint: 초기 색상 및 disabled 상태 직접 반영
/// - 충돌 시: ColorBlock의 상태 전환이 Tint 색상을 오버라이드
///
/// ===== 권장 설정 =====
/// - Transition=ColorTint: useColorBlock=true, useTint=false
/// - Transition=None: useColorBlock=false, useTint=true
/// - Transition=SpriteSwap: useColorBlock=false, useTint=true
/// </summary>
[RequireComponent(typeof(Button))]
[ExecuteAlways]
public class ThemedButton : MonoBehaviour
{
    [Header("상태별 토큰")]
    [Tooltip("Normal 상태 색상")]
    [SerializeField] private ThemeColorToken _normalToken = ThemeColorToken.Primary;
    [Tooltip("Highlighted(Hover) 상태 색상")]
    [SerializeField] private ThemeColorToken _highlightedToken = ThemeColorToken.PrimaryHover;
    [Tooltip("Pressed(클릭) 상태 색상")]
    [SerializeField] private ThemeColorToken _pressedToken = ThemeColorToken.PrimaryPressed;
    [Tooltip("Disabled 상태 색상")]
    [SerializeField] private ThemeColorToken _disabledToken = ThemeColorToken.PrimaryDisabled;

    [Header("텍스트 토큰 (선택)")]
    [Tooltip("버튼 내 TMP_Text에 색상 적용")]
    [SerializeField] private bool _applyTextColor = true;
    [Tooltip("Normal 상태 텍스트 색상")]
    [SerializeField] private ThemeColorToken _textToken = ThemeColorToken.TextOnPrimary;
    [Tooltip("Disabled 상태 텍스트 색상")]
    [SerializeField] private ThemeColorToken _textDisabledToken = ThemeColorToken.TextSecondary;

    [Header("적용 방식")]
    [Tooltip("Button.colors (ColorBlock) 설정 - Transition=ColorTint 시 사용")]
    [SerializeField] private bool _useColorBlock = true;
    [Tooltip("TargetGraphic.color 직접 설정 - 즉시 색상 반영")]
    [SerializeField] private bool _useTint = true;

    private Button _button;
    private Image _targetImage;
    private TMPro.TMP_Text _buttonText;
    private ThemeManager _cachedManager;

    public ThemeColorToken NormalToken
    {
        get => _normalToken;
        set
        {
            _normalToken = value;
            ApplyTheme();
        }
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _targetImage = _button.targetGraphic as Image;
        _buttonText = GetComponentInChildren<TMPro.TMP_Text>();
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
            _targetImage = _button.targetGraphic as Image;
            _buttonText = GetComponentInChildren<TMPro.TMP_Text>();
        }

        _cachedManager = ThemeManager.Instance;
        if (_cachedManager != null)
            _cachedManager.OnThemeChanged += ApplyTheme;

        ApplyTheme();
    }

    private void OnDisable()
    {
        // 캐싱된 참조 사용 (Instance 접근 안함 → 종료 시 새 생성 방지)
        if (_cachedManager != null)
        {
            _cachedManager.OnThemeChanged -= ApplyTheme;
            _cachedManager = null;
        }
    }

    public void ApplyTheme()
    {
        if (_button == null) return;

        Color normalColor, highlightedColor, pressedColor, disabledColor;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            normalColor = ThemeManager.GetColorInEditor(_normalToken);
            highlightedColor = ThemeManager.GetColorInEditor(_highlightedToken);
            pressedColor = ThemeManager.GetColorInEditor(_pressedToken);
            disabledColor = ThemeManager.GetColorInEditor(_disabledToken);
        }
        else
#endif
        {
            if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
                return;

            normalColor = ThemeManager.Instance.GetColor(_normalToken);
            highlightedColor = ThemeManager.Instance.GetColor(_highlightedToken);
            pressedColor = ThemeManager.Instance.GetColor(_pressedToken);
            disabledColor = ThemeManager.Instance.GetColor(_disabledToken);
        }

        // ColorBlock 방식
        if (_useColorBlock)
        {
            ColorBlock colors = _button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = disabledColor;
            colors.selectedColor = highlightedColor;
            _button.colors = colors;
        }

        // TargetGraphic Tint 방식
        if (_useTint && _targetImage != null)
        {
            _targetImage.color = _button.interactable ? normalColor : disabledColor;
        }

        // 버튼 텍스트 색상
        if (_applyTextColor && _buttonText != null)
        {
            Color textColor;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                textColor = _button.interactable
                    ? ThemeManager.GetColorInEditor(_textToken)
                    : ThemeManager.GetColorInEditor(_textDisabledToken);
            }
            else
#endif
            {
                textColor = _button.interactable
                    ? ThemeManager.Instance.GetColor(_textToken)
                    : ThemeManager.Instance.GetColor(_textDisabledToken);
            }

            _buttonText.color = textColor;
        }
    }

    /// <summary>
    /// 버튼 상태 변경 시 호출 (interactable 변경 등)
    /// </summary>
    public void RefreshState()
    {
        ApplyTheme();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
            _targetImage = _button?.targetGraphic as Image;
            _buttonText = GetComponentInChildren<TMPro.TMP_Text>();
        }

        ApplyTheme();
    }
#endif
}
