# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 프로젝트 개요

**HeartStage**는 K-POP 아이돌 테마 타워 디펜스 게임. Unity 기반, Firebase 백엔드.

### 기술 스택
| 영역 | 기술 |
|------|------|
| 엔진 | Unity 6 (6000.0.60f1) |
| 비동기 | UniTask (Cysharp) |
| 백엔드 | Firebase (Auth, Realtime DB) |
| 리소스 | Addressable Assets |
| 애니메이션 | DOTween |
| UI | Canvas + TextMeshPro |

---

## 빌드 및 테스트

### Unity 에디터에서 실행
```
File > Build Settings > Build And Run
```

### Play Mode 테스트
```
Window > General > Test Runner > Play Mode > Run All
```

### Edit Mode 테스트 (단위 테스트)
```
Window > General > Test Runner > Edit Mode > Run All
```

### 개별 테스트 실행
Test Runner 창에서 특정 테스트 클릭 후 Run Selected

---

## 폴더 구조 (Assets/Scripts/)

```
Scripts/
├── 코어 시스템
│   ├── BootStrap.cs              - 게임 초기화
│   ├── GameSceneManager.cs       - 씬 전환
│   ├── WindowManager.cs          - 윈도우 관리
│   ├── ResourceManager.cs        - Addressable 로드
│   ├── PoolManager.cs            - 객체 풀링
│   ├── SoundManager.cs           - 음성/SFX
│   ├── GenericWindow.cs          - 윈도우 기본 클래스
│   └── Defines.cs                - 상수/열거형
│
├── Animation/                    - 애니메이션
│   └── WindowAnimator.cs         - 범용 윈도우 애니메이션
│
├── Character/                    - 캐릭터 게임플레이
├── Monster/                      - 몬스터/보스 AI
├── ActiveSkill/                  - 액티브 스킬
├── Effect/                       - 상태이상/스탯 변조
│
├── Csv/                          - 데이터 테이블 (24개)
│   └── DataTableManager.cs       - 테이블 관리자
├── Data/                         - 데이터 구조체
├── Firebase/                     - Firebase 연동
│   ├── AuthManager.cs            - 인증
│   └── CloudSaveManager.cs       - 클라우드 저장
│
├── Theme/                        - 테마 시스템
│   ├── ThemeManager.cs           - 테마 싱글톤
│   ├── ThemeColorToken.cs        - 색상 토큰 (29개)
│   └── Components/               - ThemedButton, ThemedImage 등
│
└── UI/                           - UI 시스템 (가장 큼)
    ├── FriendUI/                 - 친구 시스템
    ├── Profile/                  - 프로필
    ├── Stage/                    - 스테이지
    ├── Gacha/                    - 뽑기
    ├── Shopping/                 - 상점
    ├── Quest/                    - 퀘스트
    ├── Encyclopedia/             - 도감
    ├── Mail/                     - 우편함
    └── Common/                   - 공통 (Toast, Dialog)
```

---

## 주요 시스템

### 1. 친구 시스템 (FriendUI/)
- `FriendWindow.cs` - 통합 친구 창 (목록/요청/추가 탭)
- `FriendService.cs` - 친구 추가/삭제 로직
- `DreamEnergyGiftService.cs` - 드림 에너지 선물

### 2. 상점 시스템 (Shopping/)
- `DailyShop.cs` - 24시간 리셋 데일리 상점
- `ShopItemSlot.cs` - 상점 아이템
- `PurchaseConfirmPanel.cs` - 구매 확인

### 3. 뽑기 시스템 (Gacha/)
- `GachaUI.cs` - 1회/5회 뽑기
- `GachaResultUI.cs` - 결과 표시
- 비용: 1회 50, 5회 250 하트스틱

### 4. 스테이지 시스템 (Stage/)
- `StageWindow.cs` - 지그재그 스테이지 선택
- `StageInfoWindow.cs` - 웨이브 진행도
- `CharacterSelectPanel.cs` - 캐릭터 배치

### 5. 퀘스트 시스템 (Quest/)
- 일일/주간/업적 3탭
- 7가지 이벤트: 출석, 스테이지클리어, 몬스터처치, 보스처치, 뽑기, 상점구매, 팬수달성

### 6. 도감 (Encyclopedia/)
- 포토카드 바인더 (3x2 그리드)
- 등급 강화: 연습생 → 에이스 → 신인 → 인기
- 레벨업: 트레이닝 포인트 + 라이트스틱

---

## 테마 시스템

3색 입력 → 29개 토큰 자동 생성. 상세 문서: `Assets/Scripts/Theme/README.md`

### 색상 토큰 (29개)
```
Primary (4): Primary, PrimaryHover, PrimaryPressed, PrimaryDisabled
Secondary (4): Secondary, SecondaryHover, SecondaryPressed, SecondaryDisabled
Surface (3): Surface, SurfaceAlt, Panel
Border (2): Border, Divider
Text (4): TextPrimary, TextSecondary, TextOnPrimary, TextOnSurface
Tab (4): TabActiveBg, TabInactiveBg, TabActiveText, TabInactiveText
Input (3): InputBg, InputBorder, Placeholder
Semantic (3): Success, Warning, Error
Special (2): Transparent, DimmedOverlay
```

### 사용법
```csharp
var button = GetComponent<ThemedButton>();
button.NormalToken = ThemeColorToken.Primary;
```

---

## 데이터 테이블 (24개)

| 카테고리 | 테이블 |
|----------|--------|
| 캐릭터 | CharacterTable, SkillTable, EffectTable, RankUpTable, LevelUpTable |
| 스테이지 | StageTable, StageWaveTable, MonsterTable, InfiniteStageTable, StoryTable |
| 아이템 | ItemTable, ShopTable, PieceTable |
| 뽑기 | GachaTable, GachaTypeTable |
| 퀘스트 | QuestTable, QuestTypeTable, QuestProgressTable |
| 기타 | TitleTable, SynergyTable, RewardTable, LikeabilityTable, StringTable |

---

## 씬 구조

| 씬 | 용도 |
|----|------|
| TitleScene | 타이틀/로그인 |
| LobbyScene | 로비 메인 |
| StageScene | 일반 스테이지 |
| InfinityStage | 무한 스테이지 |
| StoryScene | 스토리 |

---

## 윈도우 타입 (WindowType)

```
로비: LobbyHome, StageSelect, Gacha, Quest, Shopping, CharacterDict, SpecialDungeon
친구: Friend (30), FriendProfile (33)
인게임: VictoryDefeat, CharacterInfo, BossAlert
```

상세 목록: `Assets/Scripts/UI/OverlayPanel_Structure.md`

---

## 초기화 순서 (BootStrap.cs)

1. Addressables 초기화
2. ResourceManager 프리로드
3. DataTableManager 24개 테이블 로드
4. Firebase 로그인
5. SaveLoadManager 로드
6. 씬 전환

---

## Unity MCP 연결 (Claude Code)

### 1. Unity에서 MCP 서버 시작
1. Unity 메뉴: `Window > MCP for Unity`
2. Transport: **HTTP** 선택
3. **Start Local HTTP Server** 클릭
4. 포트 확인 (기본: 8080)

### 2. Claude Code에서 MCP 등록
```bash
# Claude CLI 경로 (VSCode Extension)
C:\Users\Kim\.vscode\extensions\anthropic.claude-code-{버전}-win32-x64\resources\native-binary\claude.exe

# MCP 서버 등록
claude mcp add --transport http UnityMCP http://localhost:8080/mcp

# MCP 서버 제거
claude mcp remove UnityMCP

# 등록된 서버 확인
claude mcp list
```

### 3. 연결 확인
- Claude Code 재시작 필요
- Unity HTTP 서버가 실행 중이어야 함

---

## 공통 UI 컴포넌트

### WindowAnimator
범용 윈도우 애니메이션 컴포넌트. 파일: `Assets/Scripts/Animation/WindowAnimator.cs`

- 지원 애니메이션: Scale, Fade, SlideUp, SlideDown, SlideLeft, SlideRight
- Origin 옵션: Center, FromButton, TopCenter, BottomCenter
- GenericWindow와 자동 연동

### ToastUI
전역 토스트 알림 시스템. 파일: `Assets/Scripts/UI/Common/ToastUI.cs`

```csharp
ToastUI.Show("메시지");
ToastUI.Show("메시지", 3f);  // 3초 표시
```

### NoteLoadingUI
전역 음표 로딩 인디케이터. 파일: `Assets/Scripts/UI/Common/NoteLoadingUI.cs`

```csharp
NoteLoadingUI.Show();    // 로딩 표시 (카운터 +1)
NoteLoadingUI.Hide();    // 로딩 숨기기 (카운터 -1)
NoteLoadingUI.ForceHide(); // 강제 숨기기 (카운터 리셋)
```

---

## OverlayPanel 구조

```
Canvas
└── OverlayPanel (Stretch All)
    ├── SharedDimmedBackground (공용 딤 배경, Left/Right/Top/Bottom: -5)
    └── 각 오버레이 창들 (FriendWindow, SettingPanel, QuestWindow 등)
```

상세 문서: `Assets/Scripts/UI/OverlayPanel_Structure.md`

---

## 프로젝트 내 문서 파일들

| 문서 | 경로 |
|------|------|
| OverlayPanel 구조 | `Assets/Scripts/UI/OverlayPanel_Structure.md` |
| 테마 시스템 | `Assets/Scripts/Theme/README.md` |
| FriendWindow 테마 매핑 | `Assets/Scripts/Theme/Editor/FriendWindowThemeMapping.md` |
| TODO 목록 | `Assets/Editor/TODO.md` |
| HTML 프로토타입 로그 | `Tools/HTML_PROTOTYPE_LOG.md` |

---

## WindowManager 오버레이/딤 시스템

### 오버레이 추적 구조
```csharp
private List<WindowType> activeOverlays;    // 모든 활성 오버레이
private HashSet<WindowType> dimmedOverlays; // 딤 배경과 함께 열린 오버레이만
```

### 오버레이 열기
- `OpenOverlay(id)` - 딤 배경과 함께 열기 (dimmedOverlays에 추가)
- `OpenOverlayNoDim(id)` - 딤 배경 없이 열기 (activeOverlays에만 추가)
- 모든 오버레이는 `isOverlayWindow = true` 설정 (Close 시 정리용)

### 딤 배경 숨기기 조건
- `dimmedOverlays.Count == 0`일 때만 딤 숨김
- 딤 없이 열린 오버레이가 남아있어도 딤은 사라짐

### 주의사항
- `CloseAllOverlays()`: 리스트 복사 후 순회 (Close가 NotifyOverlayClosed 호출하므로)
- `GenericWindow.Close()` → `NotifyOverlayClosed()` 자동 호출

---

## 스토리 던전 보상 플로우

### 관련 플래그 (SaveDataV1)
```csharp
bool showStoryRewardAfterScene;  // 씬 전환 후 보상창 표시
bool StoryAfterLobby;            // 스토리 던전 UI 복원 필요
List<int> clearedStoryStages;    // 클리어한 스토리 스테이지 (중복 보상 방지)
```

### 플로우 (전투 없는 스테이지: 66001, 66004)
1. **StoryManager.ShowStoryRewardDirectly()**
   - `showStoryRewardAfterScene = true`
   - `StoryAfterLobby = true`
   - `WindowManager.currentWindow = SpecialDungeon`
   - 로비 씬으로 이동

2. **LobbyUI.CheckReturnToStoryDungeon()**
   - StoryAfterLobby 플래그 확인
   - SpecialDungeon → StoryDungeon → StoryDungeonInfo 순차 열기
   - showStoryRewardAfterScene이면 StoryStageRewardUI 열기

3. **StoryStageRewardUI**
   - `clearedStoryStages` 체크로 중복 보상 방지
   - `_closedByUser` 플래그로 딤 클릭과 버튼 클릭 구분

### 로그아웃 시 상태 리셋
```csharp
// AuthManager.SignOutAsync()
WindowManager.currentWindow = WindowType.LobbyHome;
```
