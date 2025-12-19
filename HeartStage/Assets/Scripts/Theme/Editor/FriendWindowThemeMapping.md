# FriendWindow 테마 토큰 매핑 표

## 추천 기본 Theme (3색)

| 토큰 | Hex | RGB | 설명 |
|------|-----|-----|------|
| **BasePrimary** | `#B889D6` | (0.72, 0.54, 0.84) | 보라색 - 탭 활성, 버튼 강조 |
| **BaseSurface** | `#F5E3E8` | (0.96, 0.89, 0.91) | 핑크 라이트 - 배경 |
| **BaseText** | `#333333` | (0.2, 0.2, 0.2) | 차콜 - 본문 텍스트 |

---

## 자동 생성 규칙 요약

### 명도/채도 델타

| 파생 토큰 | 규칙 |
|-----------|------|
| PrimaryHover | 명도 +0.08 |
| PrimaryPressed | 명도 -0.12 |
| PrimaryDisabled | 채도 -0.4, 명도 +0.15 |
| Secondary | 채도 -0.15, 명도 +0.1 |
| SurfaceAlt | Surface + Primary 3% 틴트 |
| Panel | Surface + Primary 6% 틴트 |
| Border | Surface 기준 명도 0.75 |
| Divider | Surface 기준 명도 0.85 |
| TextSecondary | TextPrimary 알파 60% |
| Placeholder | TextPrimary 알파 40% |

### 대비 자동 보정

- TextOnPrimary: Primary 배경에서 대비비 4.5+ 보장 (흰색/검정/BaseText 중 선택)
- TextOnSurface: Surface 배경에서 대비 자동 계산

---

## FriendWindow 요소별 토큰 매핑

### 메인 구조

| 오브젝트 | 컴포넌트 | 원본 색상 | 토큰 |
|----------|----------|-----------|------|
| FriendWindow | Image | 투명 | Transparent |
| ShadowPanel | Image | #000000DC | DimmedOverlay |
| Scroll View | Image | #F5E3E8 | Surface |
| Viewport | Image | #FFFFFF | Surface |
| Content | - | - | - |

### 탭 버튼 (코드에서 제어)

| 상태 | 원본 색상 | 토큰 |
|------|-----------|------|
| 선택됨 배경 | #B889D6 | TabActiveBg (= Primary) |
| 선택됨 텍스트 | 자동 | TabActiveText (= TextOnPrimary) |
| 비선택 배경 | #D4A7E4 | TabInactiveBg |
| 비선택 텍스트 | 자동 | TabInactiveText |

### 장식 요소 (Sparkles)

| 오브젝트 | 원본 색상 | 토큰 | 비고 |
|----------|-----------|------|------|
| Sparkle-Small | #FFFFFF | Surface | 장식 |
| Sparkle-Medium | #FFFFFF | Surface | 장식 |
| Sparkle-Big | #FFFFFF | Surface | 장식 |
| Sparkle-Tinted-* | #FFFFFF | Surface | 틴트 이미지 |

---

## FriendWindow.cs 수정 사항

### 기존 코드 (하드코딩)

```csharp
[SerializeField] private Color selectedTabBg = new Color(0.72f, 0.54f, 0.84f, 1f);
[SerializeField] private Color unselectedTabBg = new Color(0.83f, 0.65f, 0.89f, 1f);
```

### 변경 후 (테마 참조)

```csharp
// FriendWindow.cs에서 직접 색상 참조 제거
// 탭 버튼에 ThemedButton 컴포넌트 부착

private void SetTabColor(Button button, bool isSelected)
{
    var themedButton = button.GetComponent<ThemedButton>();
    if (themedButton != null)
    {
        themedButton.NormalToken = isSelected
            ? ThemeColorToken.TabActiveBg
            : ThemeColorToken.TabInactiveBg;
        themedButton.ApplyTheme();
    }
}
```

---

## 적용 순서

1. Unity에서 `Tools > HeartStage > Theme Migration` 열기
2. `FriendWindow.prefab` 드래그
3. Theme 에셋 선택 (또는 새로 생성)
4. "색상 스캔" 클릭
5. 토큰 매핑 확인/수정
6. "매핑 확정 & 적용" 클릭

---

## 생성되는 파일

| 파일 | 경로 |
|------|------|
| Theme 에셋 | `Assets/ScriptableObject/Theme/MainTheme.asset` |
| 매핑 에셋 | `Assets/Prefabs/UI/FriendPrefap/FriendWindow_ThemeMapping.asset` |
| 검증 프로필 | `Assets/ScriptableObject/Theme/DefaultValidation.asset` |

---

## 검증 체크리스트

- [ ] TextPrimary vs Surface 대비 4.5+
- [ ] TextOnPrimary vs Primary 대비 4.5+
- [ ] Primary 사용 비율 20% 이하
- [ ] Normal→Hover→Pressed 명도 변화 확인
- [ ] Surface/Panel 비중 60% 이상
