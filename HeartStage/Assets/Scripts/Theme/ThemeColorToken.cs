/// <summary>
/// 테마 컬러 토큰 열거형 (총 29개)
/// B안: 3색(Primary/Surface/Text) 입력 → 나머지 자동 생성
///
/// 토큰 목록:
/// - Primary 계열 (4): Primary, PrimaryHover, PrimaryPressed, PrimaryDisabled
/// - Secondary 계열 (4): Secondary, SecondaryHover, SecondaryPressed, SecondaryDisabled
/// - Surface 계열 (3): Surface, SurfaceAlt, Panel
/// - Border/Divider (2): Border, Divider
/// - Text 계열 (4): TextPrimary, TextSecondary, TextOnPrimary, TextOnSurface
/// - Tab 계열 (4): TabActiveBg, TabInactiveBg, TabActiveText, TabInactiveText
/// - Input 계열 (3): InputBg, InputBorder, Placeholder
/// - Semantic (3): Success, Warning, Error
/// - Special (2): Transparent, DimmedOverlay
/// </summary>
public enum ThemeColorToken
{
    // ===== Primary (주색 계열) =====
    Primary,
    PrimaryHover,
    PrimaryPressed,
    PrimaryDisabled,

    // ===== Secondary (보조색 - Primary 기반 자동 생성) =====
    Secondary,
    SecondaryHover,
    SecondaryPressed,
    SecondaryDisabled,

    // ===== Surface (배경 계열) =====
    Surface,
    SurfaceAlt,
    Panel,

    // ===== Border / Divider =====
    Border,
    Divider,

    // ===== Text =====
    TextPrimary,
    TextSecondary,
    TextOnPrimary,
    TextOnSurface,

    // ===== Tab =====
    TabActiveBg,
    TabInactiveBg,
    TabActiveText,
    TabInactiveText,

    // ===== Input =====
    InputBg,
    InputBorder,
    Placeholder,

    // ===== Semantic (시맨틱 - 고정 팔레트) =====
    Success,
    Warning,
    Error,

    // ===== Special =====
    Transparent,
    DimmedOverlay
}
