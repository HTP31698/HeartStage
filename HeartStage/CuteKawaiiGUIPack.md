# CuteKawaiiGUIPack 에셋 가이드

이 문서는 HeartStage 프로젝트에서 사용하는 CuteKawaiiGUIPack UI 에셋의 구조와 사용법을 정리합니다.

## 에셋 경로

```
Assets/CuteKawaiiGUIPack/
├── Demo/
│   ├── Animations/     # UI 애니메이션
│   ├── Fonts/          # 폰트
│   ├── Materials/      # 머티리얼
│   ├── Prefabs/        # 프리팹 (즉시 사용 가능)
│   ├── Scenes/         # 데모 씬
│   ├── Scripts/        # 스크립트
│   └── Sprites/        # 스프라이트 (701개 PNG)
├── Documentation/      # 문서
└── Icons/              # 아이콘
```

---

## Sprites (스프라이트)

### Common (공통 요소)

| 폴더 | 설명 | 경로 |
|------|------|------|
| **Avatars** | 아바타/캐릭터 이미지 | `Sprites/Common/Avatars/` |
| **Background-Tiles** | 배경 타일 패턴 | `Sprites/Common/Background-Tiles/` |
| **Bar-Bottom** | 하단 바 | `Sprites/Common/Bar-Bottom/` |
| **Bar-Top** | 상단 바 | `Sprites/Common/Bar-Top/` |
| **Confirmation-Popup** | 확인 팝업 요소 | `Sprites/Common/Confirmation-Popup/` |
| **Effects** | 이펙트 (하트, 별, 반짝임 등) | `Sprites/Common/Effects/` |
| **Icons** | 아이콘 (화살표, 체크, 코인 등) | `Sprites/Common/Icons/` |
| **Image-Placeholder** | 이미지 플레이스홀더 | `Sprites/Common/Image-Placeholder/` |
| **Popups** | 팝업 배경/프레임 | `Sprites/Common/Popups/` |
| **Shapes** | 기본 도형 (원, 사각형 등) | `Sprites/Common/Shapes/` |
| **Title-Background** | 타이틀 배경 | `Sprites/Common/Title-Background/` |

### Panels (패널/화면)

| 폴더 | 설명 | 경로 |
|------|------|------|
| **Alerts** | 알림 UI | `Sprites/Panels/Alerts/` |
| **Character-Creation** | 캐릭터 생성 | `Sprites/Panels/Character-Creation/` |
| **Crafting** | 제작 시스템 | `Sprites/Panels/Crafting/` |
| **Credit-Shop-Popups** | 상점 팝업 | `Sprites/Panels/Credit-Shop-Popups/` |
| **Friends** | 친구 시스템 | `Sprites/Panels/Friends/` |
| **Game** | 게임 화면 | `Sprites/Panels/Game/` |
| **Home** | 홈 화면 | `Sprites/Panels/Home/` |
| **Inventory** | 인벤토리 | `Sprites/Panels/Inventory/` |
| **Messages** | 메시지/채팅 | `Sprites/Panels/Messages/` |
| **Missions** | 미션/퀘스트 | `Sprites/Panels/Missions/` |
| **Pause** | 일시정지 | `Sprites/Panels/Pause/` |
| **Profile** | 프로필 | `Sprites/Panels/Profile/` |
| **Ranking** | 랭킹 | `Sprites/Panels/Ranking/` |
| **Rewards-Ladder** | 보상 사다리 | `Sprites/Panels/Rewards-Ladder/` |
| **Settings** | 설정 | `Sprites/Panels/Settings/` |
| **Shop** | 상점 | `Sprites/Panels/Shop/` |
| **Splash** | 스플래시 화면 | `Sprites/Panels/Splash/` |

---

## Prefabs (프리팹)

### Common (공통 컴포넌트)

```
Prefabs/Common/
├── 1-Foundations/      # 기본 요소
├── 2-Components/       # UI 컴포넌트
│   ├── Buttons/        # 버튼
│   ├── Checkbox/       # 체크박스
│   ├── Dropdown/       # 드롭다운
│   ├── Input-Field/    # 입력 필드
│   ├── Progress/       # 프로그레스 바
│   ├── Radio-Button/   # 라디오 버튼
│   ├── Slider/         # 슬라이더
│   ├── Switch/         # 스위치
│   └── Toggle/         # 토글
├── 3-Layouts/          # 레이아웃
└── 4-Popups/           # 팝업
    ├── Confirmation-Popup.prefab
    └── Particles-Confetti-Gifts.prefab
```

### Panels (패널 프리팹)

```
Prefabs/Panels/
├── Alerts/
├── Character-Creation/
├── Crafting/
├── Credit-Shop-Popups/
├── Friends/
├── Game/
├── Home/
├── Inventory/
├── Messages/
├── Missions/
├── Pause/
├── Profile/
├── Ranking/
├── Rewards-Ladder/
├── Settings/
├── Shop/
└── Splash/
```

---

## Icons (아이콘 목록)

경로: `Sprites/Common/Icons/`

| 파일명 | 설명 | 색상 변형 |
|--------|------|----------|
| `Arrow-Down.png` | 아래 화살표 | Blue, White |
| `Arrow-Left.png` | 왼쪽 화살표 | Blue, White |
| `Arrow-Right.png` | 오른쪽 화살표 | Blue, White |
| `Arrow-Up.png` | 위 화살표 | Blue, White |
| `Arrows-Left-Right.png` | 좌우 화살표 | Blue, White |
| `Bunny-Head-Consenting.png` | 토끼 머리 | - |
| `Checkmark.png` | 체크 표시 | - |
| `Clock.png` | 시계 | - |
| `Coin.png` | 코인 | - |
| `Empty.png` | 빈 아이콘 | - |
| `Error.png` | 에러 | - |
| `Gem.png` | 보석 | - |
| `Gift.png` | 선물 | - |
| `Info.png` | 정보 | - |
| `Lock.png` | 잠금 | - |
| `Paint-Brush.png` | 페인트 브러시 | - |
| `Star-XP.png` | 경험치 별 | - |

---

## Shapes (기본 도형)

경로: `Sprites/Common/Shapes/`

| 파일명 | 설명 |
|--------|------|
| `Circle.png` | 원형 |
| `Rectangle.png` | 사각형 |
| `Rectangle-Outline.png` | 사각형 외곽선 |
| `Square.png` | 정사각형 |
| `Squircle.png` | 둥근 사각형 |

---

## Animations (애니메이션)

### Effects (이펙트 애니메이션)

| 이름 | 경로 | 설명 |
|------|------|------|
| **Clouds-Horizon** | `Animations/Common/Effects/Clouds-Horizon/` | 구름 애니메이션 |
| **Heart** | `Animations/Common/Effects/Heart/` | 하트 애니메이션 |
| **Locks** | `Animations/Common/Effects/Locks/` | 잠금 애니메이션 |
| **Rotation-360** | `Animations/Common/Effects/Rotation-360/` | 360도 회전 |
| **Scale** | `Animations/Common/Effects/Scale/` | 스케일 애니메이션 |
| **Sparkles** | `Animations/Common/Effects/Sparkles/` | 반짝임 (8종) |
| **Swing** | `Animations/Common/Effects/Swing/` | 흔들림 |
| **Tiles** | `Animations/Common/Effects/Tiles/` | 타일 애니메이션 |

### UI 애니메이션

| 이름 | 경로 | 설명 |
|------|------|------|
| **Button** | `Animations/Common/UI/Button/` | 버튼 애니메이션 |
| **Panel** | `Animations/Common/UI/Panel/` | 패널 애니메이션 |
| **Popup** | `Animations/Common/UI/Popup/` | 팝업 열기/닫기 |

---

## 사용 예시

### 스프라이트 경로 (MCP용)

```
// 버튼 배경
Assets/CuteKawaiiGUIPack/Demo/Sprites/Common/Shapes/Squircle.png

// 잠금 아이콘
Assets/CuteKawaiiGUIPack/Demo/Sprites/Common/Icons/Lock.png

// 코인 아이콘
Assets/CuteKawaiiGUIPack/Demo/Sprites/Common/Icons/Coin.png

// 체크 아이콘
Assets/CuteKawaiiGUIPack/Demo/Sprites/Common/Icons/Checkmark.png
```

### 프리팹 경로

```
// 확인 팝업
Assets/CuteKawaiiGUIPack/Demo/Prefabs/Common/4-Popups/Confirmation-Popup.prefab

// 버튼 컴포넌트
Assets/CuteKawaiiGUIPack/Demo/Prefabs/Common/2-Components/Buttons/
```

---

## UI 개발 시 권장 사항

1. **패널/팝업**: `Prefabs/Panels/` 또는 `Prefabs/Common/4-Popups/` 참고
2. **버튼**: `Prefabs/Common/2-Components/Buttons/` 프리팹 복사 후 수정
3. **아이콘**: `Sprites/Common/Icons/` 에서 선택
4. **배경**: `Sprites/Common/Shapes/` 의 Squircle 권장 (둥근 사각형)
5. **애니메이션**: `Animations/Common/Effects/` 의 Scale, Sparkles 활용

---

## 특수 던전 UI 적용 가이드

DungeonItemUI에 적용 가능한 요소:

| UI 요소 | 권장 스프라이트/프리팹 |
|---------|----------------------|
| 배경 | `Shapes/Squircle.png` |
| 잠금 오버레이 | `Icons/Lock.png` |
| 입장 버튼 | `Prefabs/Common/2-Components/Buttons/` |
| 확장 패널 배경 | `Shapes/Rectangle.png` |
| 보상 아이콘 | `Icons/Coin.png`, `Icons/Gem.png` |
