# UI Theme - Theme System v2

3색 입력 → 29개 토큰 자동 생성 테마 시스템

---

## 빠른 시작 (Quick Start)

### Step 1: Theme 에셋 만들기
1. Project 창에서 우클릭
2. **Create > HeartStage > Theme** 선택
3. 3가지 색상 설정:
   - **BasePrimary**: 주색 (버튼, 강조 등)
   - **BaseSurface**: 배경색
   - **BaseText**: 글자색
4. Inspector에서 색상 변경하면 자동으로 29개 토큰 생성됨

### Step 2: 기존 프리팹 마이그레이션
1. **Tools > UI Theme > Theme Migration** 실행
2. 프리팹 드래그 → 테마 선택
3. "색상 스캔" 클릭
4. 토큰 매핑 확인/수정 → "매핑 확정 & 적용"

### Step 3: 새 UI에 테마 적용
UI 오브젝트에 Themed 컴포넌트 부착:
- Image → **ThemedImage** 컴포넌트 추가
- TMP_Text → **ThemedTMPText** 컴포넌트 추가
- Button → **ThemedButton** 컴포넌트 추가

### Step 3.5: 장식 요소에 Tint 모드 사용
배경 패턴, 구름, 장식 등 **원본 색감을 유지**해야 하는 요소:
1. ThemedImage 컴포넌트 추가
2. **Apply Mode** → `Tint` 선택
3. **Tint Strength** 조절 (0.10~0.35 권장)
4. 필요시 "현재 색상 캡처" 버튼으로 원본 색상 저장

### Step 4: 하드코딩 색상 검사 (선택)
1. **Tools > UI Theme > Theme Audit** 실행
2. 폴더 지정 → 스캔
3. Themed 컴포넌트 없는 UI 요소 확인

---

## 핵심 구조

```
ThemeColorGenerator  →  Theme (SO)  →  Themed* 컴포넌트
    (3색 → 29토큰)         (팔레트)         (런타임 적용)
```

## 색상 토큰 (29개)

### Primary 계열 (4)
- Primary, PrimaryHover, PrimaryPressed, PrimaryDisabled

### Secondary 계열 (4)
- Secondary, SecondaryHover, SecondaryPressed, SecondaryDisabled

### Surface 계열 (3)
- Surface, SurfaceAlt, Panel

### Border 계열 (2)
- Border, Divider

### Text 계열 (4)
- TextPrimary, TextSecondary, TextOnPrimary, TextOnSurface

### Tab 계열 (4)
- TabActiveBg, TabInactiveBg, TabActiveText, TabInactiveText

### Input 계열 (3)
- InputBg, InputBorder, Placeholder

### Semantic (3)
- Success, Warning, Error

### Special (2)
- Transparent, DimmedOverlay

## 파일 구조

```
Assets/Scripts/Theme/
├── ThemeColorGenerator.cs      # 색상 생성 로직
├── Theme.cs                    # Theme SO
├── ThemeColorToken.cs          # 29개 토큰 enum
├── PrefabThemeMapping.cs       # 프리팹별 매핑 저장
├── Components/
│   ├── ThemedImage.cs          # Image 색상 적용
│   ├── ThemedTMPText.cs        # TMP_Text 색상 적용
│   ├── ThemedButton.cs         # Button ColorBlock 적용
│   ├── ThemedOutlineShadow.cs  # Outline/Shadow 적용
│   ├── ThemeBackgroundMarker.cs# 배경 마킹 컴포넌트
│   ├── ThemeIgnoreValidation.cs# 검증 예외 지정
│   └── ThemeOverrideToken.cs   # 토큰 강제 지정
├── Validation/
│   ├── ThemeValidator.cs       # 검증 로직
│   └── ThemeValidationProfile.cs# 검증 룰 설정 (SO)
└── Editor/
    ├── ThemeMigrationWindow.cs # 마이그레이션 도구
    ├── ThemeAuditWindow.cs     # 하드코딩 탐지 도구
    └── Tests/
        └── ThemeGoldenTests.cs # 골든 테스트
```

## 에디터 도구

### Theme Migration (Tools > UI Theme > Theme Migration)
- 기존 프리팹을 테마 시스템으로 변환
- 색상 스캔 → 토큰 추천 → 승인 → Themed 컴포넌트 부착
- Nested Prefab 옵션 지원 (기본 OFF)
- **재적용 정책**: 이미 Themed 컴포넌트가 있으면 스킵 (기존 토큰 유지)

### Theme Audit (Tools > UI Theme > Theme Audit)
- 프로젝트에서 하드코딩된 색상 탐지
- Prefab, Scene 대상 스캔
- JSON/CSV 리포트 Export

### Theme Golden Tests (Tools > UI Theme > Run Theme Golden Tests)
- Generator 결정성 검증
- Validator 룰 동작 검증

## 검증 룰

| 룰 | Severity | 기준 |
|----|----------|------|
| 텍스트 대비 (본문/버튼/Tab) | **Error** | WCAG 4.5:1 (Small), 3:1 (Large) |
| Semantic 대비 | Warning | Surface 대비 3:1 |
| 상태 델타 | Warning | Hover ≥0.05, Pressed ≥0.08 |
| 포인트 예산 | Warning | Primary ≤ 20% (컴포넌트 수 기준, 가중치 옵션 시 가중치 합산) |
| 시맨틱 오용 | Warning | Semantic 토큰이 장식/중립 요소에 사용되면 경고 (개수는 보조 지표) |
| 뉴트럴 부족 | Warning | Surface/Panel ≥ 60% |
| 60-30-10 | Warning (옵션) | 비율 허용 오차 내 |

## 우선순위 체계

### 토큰 결정 우선순위 (색상 적용)
1. **ThemeOverrideToken** (최우선 - 강제 토큰 지정)
2. Themed 컴포넌트 토큰 설정
3. 자동 추천 (마이그레이션 기본값)

### 검증 제외
- **ThemeIgnoreValidation**은 토큰 결정과 무관, Validator 단계에서만 룰 제외

## 가중치 계산 (옵션)

`ThemeValidationProfile.UseContainerWeights = true` 시:

| BackgroundType | 가중치 |
|----------------|--------|
| FullScreen | 5.0 |
| Panel | 3.0 |
| Modal | 3.0 |
| Container | 2.0 |
| ListItem | 1.5 |
| (마커 없음) | 1.0 |

## 색공간 규칙 (ColorSpace Guard)

- **명도/채도 연산**: HSV (sRGB 기준, Unity 내장)
- **Tint/Lerp**: sRGB 채널별 선형 보간 (공간 변환 없음)
- **WCAG 휘도**: sRGB → Linear 변환 후 계산
- **결정성 보장**: 입력 3색 동일 → 토큰 동일 (프로젝트 Color Space 무관)
- **렌더링 주의**: 최종 시각 결과는 프로젝트 파이프라인(Linear/Gamma)에 따라 차이 가능

## ThemedImage 모드

### Solid 모드 (기본)
- 테마 색상으로 **완전 대체**
- 버튼, 패널, 배경 등 단색 UI에 적합

### Tint 모드 (장식용)
- 원본 색상에 테마 색조를 **블렌딩**
- `result = Lerp(originalColor, themeColor, tintStrength)`
- 패턴, 구름, 장식 등 원본 아트 유지 필요 시 사용

| 용도 | 권장 Token | 권장 Strength |
|------|------------|---------------|
| 배경 패턴 (토끼 등) | Primary | 0.08 ~ 0.15 |
| 구름/프레임 장식 | Primary/Secondary | 0.18 ~ 0.30 |
| 강조 장식/뱃지 | Primary | 0.30 ~ 0.45 |

## 지원 범위

### 지원
- Image.color (Solid / Tint 모드)
- TMP_Text.color
- Button ColorBlock (Normal/Highlighted/Pressed/Disabled)
- Outline/Shadow effectColor

### 미지원
- **TMP Material (Outline/Underlay)**: 공유 재질 이슈로 기본 미지원
  - 필요 시 별도 확장 컴포넌트로 제공 가능
- **UI Toolkit/USS**: 범위 밖
- Gradient 색상
- SpriteRenderer Tint
- Shader 색상 프로퍼티
