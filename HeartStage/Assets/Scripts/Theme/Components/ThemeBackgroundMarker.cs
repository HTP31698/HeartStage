using UnityEngine;

/// <summary>
/// 배경/패널 역할 마커 컴포넌트
/// Primary 배경 오용 탐지 시 휴리스틱(이름 기반) 대신 명시적 마킹 사용
///
/// ===== 사용법 =====
/// 1. 배경/패널 역할의 Image에 이 컴포넌트 추가
/// 2. ThemeValidator가 자동으로 감지하여 Primary 사용 시 경고
/// 3. 의도적으로 Primary를 배경에 사용하려면 ThemeIgnoreValidation 함께 사용
///
/// ===== 탐지 우선순위 =====
/// 1. ThemeBackgroundMarker 컴포넌트 (명시적)
/// 2. 이름 기반 휴리스틱 (bg, panel, background, container)
/// </summary>
[DisallowMultipleComponent]
public class ThemeBackgroundMarker : MonoBehaviour
{
    [Tooltip("배경 유형")]
    public BackgroundType Type = BackgroundType.Panel;

    [Tooltip("메모 (검증 리포트에 표시)")]
    [TextArea(1, 2)]
    public string Note;
}

/// <summary>
/// 배경 유형
/// </summary>
public enum BackgroundType
{
    /// <summary>메인 배경 (화면 전체)</summary>
    FullScreen,

    /// <summary>패널/카드 배경</summary>
    Panel,

    /// <summary>모달/팝업 배경</summary>
    Modal,

    /// <summary>컨테이너 배경</summary>
    Container,

    /// <summary>리스트 아이템 배경</summary>
    ListItem
}
