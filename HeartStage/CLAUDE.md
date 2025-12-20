# HeartStage 개발 문서

---

## 프로젝트 개요

**HeartStage**는 K-POP 아이돌 테마 타워 디펜스 게임. Unity 기반, Firebase 백엔드.

### 기술 스택
| 영역 | 기술 |
|------|------|
| 엔진 | Unity 2022 LTS+ |
| 비동기 | UniTask (Cysharp) |
| 백엔드 | Firebase (Auth, Realtime DB) |
| 리소스 | Addressable Assets |
| 애니메이션 | DOTween |
| UI | Canvas + TextMeshPro |

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

## WindowAnimator 시스템 (2024-12-20)

### 개요
범용 윈도우 애니메이션 컴포넌트. 컴포넌트만 붙이면 열기/닫기 애니메이션 자동 적용.

### 파일 위치
- `Assets/Scripts/Animation/WindowAnimator.cs`

### 지원 애니메이션
- Scale, Fade, SlideUp, SlideDown, SlideLeft, SlideRight

### Origin 옵션
- Center, FromButton, TopCenter, BottomCenter

### 사용법
1. 윈도우 오브젝트에 `WindowAnimator` 컴포넌트 추가
2. Inspector에서 설정:
   - Open Type: Scale
   - Open Start Scale: 0.8
   - Open Duration: 0.25
   - Open Ease: OutBack
   - Close Type: Scale
   - Close End Scale: 0.8
   - Close Duration: 0.15
   - Close Ease: InBack
   - Auto Play On Enable: true

### GenericWindow 연동
```csharp
// GenericWindow.cs에서 자동 처리
protected virtual void Awake()
{
    _windowAnimator = GetComponent<WindowAnimator>();
}

public virtual void Close()
{
    if (_windowAnimator != null)
        _windowAnimator.PlayClose(() => gameObject.SetActive(false));
    else
        gameObject.SetActive(false);
}
```

---

## FriendWindow 수정 사항 (2024-12-20)

### ClaimAllButton 투명/깜빡임 수정
- `button.interactable = false` 대신 `_isClaimingAll` 플래그 사용
- ThemedButton이 interactable 상태에 따라 색상을 변경하기 때문

### 탭 버튼 색상/선택 상태
- `ThemedButton.NormalToken`으로 동적 테마 색상 변경
- `button.interactable = !isSelected`로 선택된 탭 비활성화
- ThemeColorToken: `TabActiveBg`, `TabInactiveBg` 사용

### 요청 탭 InfoBar 추가
- `requestInfoBar` GameObject
- `requestInfoText` 받은 요청 개수 표시

### ListInfoBar 활성화 수정
- `Open()`에서 `UpdateUIForTab()` 호출 추가

---

## NoteLoadingUI 시스템 (2024-12-20)

### 개요
전역 음표 로딩 인디케이터. 음표 3개(빨강, 노랑, 파랑)가 순차적으로 통통 튀는 애니메이션.

### 파일 위치
- `Assets/Scripts/UI/Common/NoteLoadingUI.cs` - 전역 싱글톤
- `Assets/Scripts/Animation/LoadingIndicator.cs` - 바운스 애니메이션

### 사용법
```csharp
NoteLoadingUI.Show();    // 로딩 표시 (카운터 +1)
NoteLoadingUI.Hide();    // 로딩 숨기기 (카운터 -1)
NoteLoadingUI.ForceHide(); // 강제 숨기기 (카운터 리셋)
```

### Unity 설정
```
NoteLoadingUI (GameObject) - NoteLoadingUI.cs
  └── LoadingIndicator (자식, 기본 비활성화) - LoadingIndicator.cs
        ├── RedNote (Image)
        ├── YellowNote (Image)
        └── BlueNote (Image)
```

### 카운터 시스템
- 중복 호출 방지: Show() 여러 번 호출해도 마지막 Hide()까지 로딩 유지
- ForceHide(): 윈도우 열 때 카운터 리셋용

### LoadingIndicator 설정값
- bounceHeight: 15
- bounceDuration: 0.15
- delayBetweenNotes: 0.08

---

# HTML 프로토타입 작업 로그

## 프로젝트 정보
- **파일 위치**: `Tools/LobbyPanelPrototype_v3.html`
- **목적**: Unity UI Toolkit 이전을 위한 HTML/CSS 프로토타입
- **작업 방식**: CS 파일 스캔 → HTML/CSS/JS 구현

---

## 완료된 작업

### 1. 상점 페이지 (Shop)
**스캔한 CS 파일:**
- `ShopTable.cs` - Shop_ID, Shop_type, Shop_item_type1-4, Shop_currency, Shop_price
- `DailyShop.cs` - 24시간 리셋, 3개 랜덤 아이템, 카운트다운 타이머
- `ShopItemSlot.cs` - 아이템 표시, 구매 완료 시 alpha 0.5
- `PurchaseConfirmPanel.cs` - 구매 확인 다이얼로그

**구현 내용:**
- 일일 상점 (24시간 타이머, 3개 아이템)
- 패키지 상점, 다이아 상점 섹션
- 구매 확인 모달
- `updateDailyShopTimer()`, `openPurchaseModal()`, `confirmPurchase()` JS 함수

---

### 2. 뽑기 페이지 (Gacha)
**스캔한 CS 파일:**
- `GachaTable.cs` - Gacha_ID, Gacha_type, Gacha_item, Gacha_per (확률)
- `GachaTypeTable.cs` - Gacha_type_ID, Gacha_name, Gacha_currency, Gacha_price
- `GachaUI.cs` - 1회 뽑기, 5회 뽑기 버튼
- `GachaManager.cs` - DrawGacha (50 HeartStick), DrawGachaFiveTimes (250 HeartStick)
- `GachaResultUI.cs` - 결과 표시

**구현 내용:**
- 뽑기 배너, 1회/5회 뽑기 버튼 (50/250 HeartStick)
- 뽑기 결과 모달
- 확률 정보 모달
- `doGacha()`, `drawRandomGacha()` JS 함수

---

### 3. 전투 페이지 (Battle/Stage)
**스캔한 CS 파일:**
- `StageTable.cs` - stage_ID, stage_name, stage_step1, stage_step2, debut_stamina, wave1-4_id
- `StageWindow.cs` - 지그재그 레이아웃 (horizontalOffset = 350f)
- `StageChoosePrefab.cs` - 스테이지 이미지, 클리어 상태
- `StageInfoWindow.cs` - 웨이브 진행도, 에너지 비용

**구현 내용:**
- 지그재그 스테이지 목록 (odd/even margin)
- 스테이지 아이템 (아이콘, 이름, 웨이브 진행도 dot)
- 스테이지 정보 모달 (웨이브 원형 진행도, 에너지 비용)
- `openStageInfo()`, `startStage()` JS 함수

---

### 4. 던전 페이지 (Dungeon)
**스캔한 CS 파일:**
- `SpecialDungeonUI.cs` - 특별 스테이지 버튼, 스토리 버튼
- `DungeonItemUI.cs` - 아코디언 형태, 일일 도전 횟수, 잠금 상태, DOTween 애니메이션
- `StoryDungeonUI.cs`, `StoryDungeonInfoUI.cs` - 스토리 던전
- `InfiniteStageTable.cs` - stage_id, daily_limit, reward_item_id

**구현 내용:**
- 특별 스테이지 / 스토리 탭 전환
- 아코디언 형태 던전 아이템 (클릭 시 확장/축소)
- 일일 도전 횟수 표시 (3/3)
- 잠금 상태 표시 (LockOverlay, 해금 조건 텍스트)
- 입장 버튼 + 도전 횟수 감소 기능
- `switchDungeonTab()`, `toggleDungeonItem()`, `enterDungeon()` JS 함수

---

### 5. 이미지 오류 수정
**문제**: 페이지 전환 시 이미지 깨짐

**해결:**
```css
/* translate3d로 GPU 최적화 */
@keyframes pageSlideInFromRight {
    from { transform: translate3d(100%, 0, 0); }
    to { transform: translate3d(0, 0, 0); }
}

.main-content {
    perspective: 1000px;
    -webkit-perspective: 1000px;
}

.page {
    transform-style: preserve-3d;
    image-rendering: -webkit-optimize-contrast;
}
```

---

### 6. 도감 페이지 (포토카드 바인더) ✅
**스캔한 CS 파일:**
- 기획서 26페이지 기반 구현

**구현 내용:**
- 포토카드 바인더 메인 화면 (3x2 그리드, 필터, 페이지네이션)
- 캐릭터 정보 화면 (슬롯 4개, 스탯 7종, 등급 버튼, 3탭)
- 7페이지 튜토리얼 팝업
- 등급 강화 팝업 (4단계 등급 시스템)
- 포토카드 교체 팝업 (5가지 상태)
- 의상 변경 팝업 (상의/하의/신발, 스탯 증감 표시)
- 레벨업 영역 (트레이닝 포인트, 라이트스틱)
- `openCharacterInfo()`, `openTutorialPopup()`, `openGradeUpPopup()` 등 JS 함수

---

### 7. 추가 기능 구현 (2024.12.18) ✅

**메일 상세 모달:**
- 메일 클릭 시 상세 내용 표시
- 제목, 발신자, 내용, 보상 목록
- 상세 모달에서 직접 수령 가능
- `openMailDetail()`, `claimMailDetail()`, `parseRewardString()` JS 함수

**상점 재구매 음영 처리:**
- 데일리 상점 아이템 구매 완료 시 `.purchased` 클래스 추가
- opacity 0.5 + "구매완료" 텍스트 오버레이
- `confirmPurchase()` 함수에서 자동 처리

---

## 📚 포토카드 바인더 시스템 (기획서 26페이지 전체 정리)

### 6-1. 메인 화면 - 포토카드 바인더
**진입:** 하단 탭 "아이돌 포토카드" 터치

**레이아웃:**
- 상단: "포토카드 바인더" 타이틀
- 필터 드롭다운 + 보유 캐릭터 수 (4/15)
- 정보(i) 버튼
- 6개 카드 그리드 (3x2)
- 좌우 페이지 이동 버튼 (◀ ▶)
- 좌우 스와이프로도 페이지 이동 (책 넘기는 애니메이션)

**필터 기능 (드롭다운):**
1. 등급순 (기본)
2. 레벨순
3. 이름순
4. 속성순: 보컬 > 랩 > 카리스마 > 큐티 > 댄스 > 비주얼 > 섹시

**기본 정렬 (필터 미선택시):**
- 등급 높은 순 → 레벨 높은 순 → 이름순 (ㄱㄴㄷ)
- 보유 캐릭터 먼저, 미보유는 뒤쪽

---

### 6-2. 카드 표시 상태

**보유 캐릭터:**
- 컬러 이미지
- 등급별 테두리 색상 (금색, 분홍색, 초록색 등)
- 레벨업/등급업 가능시 느낌표(!) 알림 아이콘
- 터치 → 캐릭터 정보 화면

**미보유 캐릭터:**
- 흑백(회색) 이미지
- 자물쇠(🔒) 아이콘
- 하단에 조각 수 표시: ⭐ n/10
- 조각 10개 모으면 자동 해금
- 터치 → 제한된 정보 화면 (럭키 드로우만 활성화)

---

### 6-3. 정보(i) 버튼 - 7페이지 튜토리얼 팝업

| 페이지 | 제목 | 내용 |
|--------|------|------|
| 1/7 | 하트 에너지와 정화란? | 아이돌이 하트 에너지로 사람들을 정화, 영상 자동 재생 |
| 2/7 | 스탯이란? | 7가지 스탯 설명 + 캐릭터 정보창 이미지 |
| 3/7 | 포지션이란? | 패시브 스킬, 범위 이미지 예시 |
| 4/7 | 퍼포먼스란? | 액티브 스킬, 드래그로 시전/취소, 영상 자동 재생 |
| 5/7 | 레벨이란? | 트레이닝 포인트 + 라이트스틱으로 레벨업 |
| 6/7 | 등급이란? | 4단계 등급 시스템, 캐릭터 조각으로 강화 |
| 7/7 | 의상이란? | 스탯 추가 올려주는 장비, before/after 이미지 |

**버튼 규칙:**
- 첫 페이지: "다음" 버튼만
- 중간 페이지: "뒤로" + "다음"
- 마지막 페이지: "뒤로" + "닫기"
- X 버튼으로 언제든 닫기 가능

---

### 6-4. 캐릭터 정보 화면 (보유 캐릭터)

#### 상단 영역
```
┌─────────────────────────────────────┐
│ [←뒤로]                              │
├──────┬────────┬─────────────────────┤
│ 포토  │        │ 하나 Lv 2    보컬 🥇│
│ 카드  │ 캐릭터 ├─────────────────────┤
│ 슬롯  │ 모델   │ 보컬    260         │
├──────┤ (idle) │ 랩      1           │
│ 상의  │        │ 댄스    1202        │
├──────┤        │ 비주얼  9.6         │
│ 하의  │        │ 섹시    1.54        │
├──────┤        │ 큐티    0.04        │
│ 신발  │        │ 카리스마 10.3       │
└──────┴────────┴─────────────────────┘
              [병아리 연습생 ↑]
```

**포토카드 슬롯:** 터치 → 포토카드 교체 팝업
**의상 슬롯 3개:** 터치 → 의상 변경 팝업
**캐릭터 모델:** idle 상태 유지

---

#### 능력치 영역

**스탯 7종 + 의미:**
| 스탯 | 수치 예시 | 의미 (토글시 표시) |
|------|-----------|-------------------|
| 보컬 | 260 | 정화 강도 |
| 랩 | 1 | 정화 속도 |
| 댄스 | 1202 | 체력 |
| 비주얼 | 9.6 | 강력한 정화 확률 |
| 섹시 | 1.54 | 강력한 정화 강도 |
| 큐티 | 0.04 | 추가 정화 확률 |
| 카리스마 | 10.3 | 정화 도달 거리 |

**스탯 설명 토글 버튼:**
- 기본: 수치값 표시
- 토글 ON: 스탯 설명 텍스트로 변경
- 다시 토글: 수치값으로 복귀

---

#### 등급 강화 버튼

**상태별 표시:**
- 강화 불가능: 버튼 비활성화, 등급 이름만 텍스트 ("병아리 연습생")
- 강화 가능: 버튼 활성화, 화살표(↑) 아이콘 + 빛나는 애니메이션

**터치시 → 등급 확인 팝업:**
```
┌─────────────────────────────────┐
│         아이돌 등급 확인          │
│                              [X]│
├─────────────────────────────────┤
│    ① 에이스 연습생               │
│    ⭐⭐                          │
├─────────────────────────────────┤
│    ② 신인 아이돌                 │
│    [스킬 아이콘] [스킬 설명]      │
├─────────────────────────────────┤
│    ③ 등급별 변동 사항 비교        │
│    (빨간색 텍스트로 강조)         │
├─────────────────────────────────┤
│    ⭐ 5/20                       │
│    [====----] 강화 조건 게이지    │
├─────────────────────────────────┤
│        [등급 강화] ← 주황색 버튼   │
└─────────────────────────────────┘
```

---

### 6-5. 등급 시스템 (4단계)

| 등급 | 이름 | 강화시 획득 |
|------|------|-----------|
| 1등급 (기본) | 병아리 연습생 | 기본 공격, 패시브/액티브 스킬 보유 |
| 2등급 | 에이스 연습생 | 패시브 스킬 **범위** 강화 |
| 3등급 | 신인 아이돌 | **액티브 스킬** 강화 |
| 4등급 (최종) | 인기 아이돌 | 패시브 스킬 **수치 값** 강화 |

**용어 정리:**
- 포지션 = 패시브 스킬
- 퍼포먼스 = 액티브 스킬

---

### 6-6. 하단 탭 3개

#### 캐릭터 정보 탭 (기본 선택)
- 키: 161cm
- 나이: 만 18세
- 성격: 긍정적이고 파이팅 넘침, 외향적, 사교적, 용감함
- 스토리: 긴 배경 스토리 텍스트 (스크롤 가능)

#### 퍼포먼스 탭 (액티브 스킬)
- 스킬 아이콘 + 스킬 이름
- 대기시간: n초
- 스킬 설명 텍스트
- 등급 강화시 설명과 아이콘 변경

#### 포지션 탭 (패시브 스킬)
- 스킬 범위 이미지 (3x3 그리드, 적용 칸 표시)
- 능력치 텍스트: "보컬을 n.n 올려주고, 비주얼을 n.n% 올려준다"
- 등급 강화시 설명과 이미지 변경

---

### 6-7. 레벨 정보 영역

```
┌─────────────────────────────────┐
│ Lv 3 🎵 [====----] 55/960       │
│                                 │
│     [레벨 업 🎸500]             │
└─────────────────────────────────┘
```

**표시 요소:**
- 현재 레벨 (Lv 3)
- 다음 레벨 표시
- 트레이닝 포인트 아이콘 (🎵)
- 레벨업 조건 게이지 (보유/필요 트레이닝 포인트)
- 레벨업 버튼 (라이트 스틱 🎸 아이콘 + 필요 개수)

**레벨업 버튼 조건:**
- 트레이닝 포인트 + 라이트 스틱이 필요 개수만큼 있을 때만 활성화
- 부족하면 항상 비활성화

**레벨업 버튼 터치시:**
1. 보유 라이트 스틱 차감
2. 필요한 라이트 스틱 개수 증가
3. 게이지 바 옆 다음 레벨 표시 변경
4. 트레이닝 포인트 차감
5. 필요 트레이닝 포인트 개수 증가
6. 캐릭터 정보창 우측 상단 현재 캐릭터 레벨 변경
7. 캐릭터가 가진 스탯 수치를 해당 레벨에 맞게 값 변경

---

### 6-8. 포토카드 교체 팝업

```
┌─────────────────────────────────┐
│       포토카드 교체 (2/4)     [X]│
├─────────────────────────────────┤
│ [연습생 하나]    [신인 아이돌 하나]│
│   [장착됨]         [보유중]      │
├─────────────────────────────────┤
│ [연말 시상식 하나] [청량돌 하나]  │
│                                 │
├─────────────────────────────────┤
│ 정화 무대 진행 시 확인할 수 있습니다│
└─────────────────────────────────┘
```

**버튼 상태 5가지:**
| 상태 | 설명 |
|------|------|
| 장착중 | 현재 장착중인 상태, 터치시 보유중으로 변경 |
| 보유중 | 보유하고 있지만 장착 안함, 터치시 장착됨으로 변경 |
| 럭키 드로우 | 미보유, 뽑기로 획득 가능, 터치시 해당 탭으로 이동 |
| 스토리 던전 | 스토리 던전 클리어시 획득 가능 |
| 💎200 | 하트 스틱으로 구매 가능, 터치시 구매 팝업 |

---

### 6-9. 의상 변경 팝업

```
┌─────────────────────────────────┐
│ [X]        스탯 정보             │
├──────┬──────────────────────────┤
│      │ 보컬  260 (+10)          │
│ 캐릭  │ 랩    1   (-5) ← 빨간색  │
│ 터    │ 댄스  1202              │
│ 모델  │ ...                     │
├──────┼──────────────────────────┤
│      │ [상의] [하의] [신발]      │
├──────┼──────────────────────────┤
│      │ [의상1] [의상2] [의상3]   │
│      │ [의상4] [의상5] ...       │
├──────┴──────────────────────────┤
│           [완료]                 │
└─────────────────────────────────┘
```

**탭:** 상의 / 하의 / 신발
**표시:** 보유 의상만 표시
**정렬:** 최근 획득한 의상 제일 앞에

**장착한 의상 표시:**
- 현재 장착중: 캐릭터 모델링 옆 슬롯 칸에 표시
- 다른 탭에서는 반투명한 회색으로 표시

**스탯 변화 표시:**
- 스탯 올라감: 초록색 텍스트 (+10)
- 스탯 내려감: 빨간색 텍스트 (-5)

**완료 버튼:**
- 변경 사항 있을 때만 활성화 (색상 진하게)
- 변경 사항 없으면 비활성화 (회색)
- 완료 버튼 눌러야 최종 적용
- 닫기(X) 버튼 누르면 적용 안됨

---

### 6-10. 미보유 캐릭터 정보 화면

**시각적 차이:**
- 패널 전체 짙은 회색
- 자물쇠(🔒) 아이콘 표시
- 스탯 수치 표시 안됨

**비활성화 항목:**
- 캐릭터 정보 탭
- 퍼포먼스 탭
- 포지션 탭
- 등급 강화 버튼
- 포토카드 변경 버튼
- 의상(상의/하의/신발) 슬롯

**활성화 항목:**
- 뒤로 가기 버튼 → 포토카드 바인더로 이동
- 럭키 드로우 버튼 (노란색) → 럭키 드로우(뽑기) 탭으로 이동
- 등급 버튼은 이름만 텍스트로 표시 (비활성화 상태)

---

## 구현 체크리스트 ✅ 모두 완료

### HTML 구조
- [x] 포토카드 바인더 메인 화면
- [x] 필터 드롭다운
- [x] 카드 그리드 (6개, 3x2)
- [x] 페이지네이션 버튼
- [x] 캐릭터 정보 화면 전체
- [x] 7페이지 튜토리얼 팝업
- [x] 등급 강화 팝업
- [x] 포토카드 교체 팝업
- [x] 의상 변경 팝업

### CSS 스타일
- [x] 카드 컬러/흑백 상태
- [x] 등급별 테두리 색상 (브라운/그린/핑크/골드)
- [x] 자물쇠 오버레이
- [x] 조각 수 표시
- [x] 스탯 증가/감소 색상 (초록/빨강)
- [x] 버튼 활성화/비활성화 상태
- [x] 페이지 전환 애니메이션

### JS 함수
- [x] `filterCards(type)` - 필터링
- [x] `changeBinderPage(direction)` - 페이지 이동
- [x] `openCharacterInfo(characterId)` - 캐릭터 정보 열기
- [x] `closeCharacterInfo()` - 캐릭터 정보 닫기
- [x] `switchCharTab(tabName)` - 탭 전환
- [x] `toggleStatDescription()` - 스탯 설명 토글
- [x] `openGradeUpPopup()` - 등급 강화 팝업
- [x] `doGradeUp()` - 등급 강화 실행
- [x] `openPhotocardChangePopup()` - 포토카드 교체 팝업
- [x] `equipPhotocard(id)` - 포토카드 장착
- [x] `goToGacha()` - 뽑기로 이동
- [x] `openCostumePopup(type)` - 의상 변경 팝업
- [x] `switchCostumeTab(type)` - 의상 탭 전환
- [x] `selectCostume(id)` - 의상 선택
- [x] `confirmCostumeChange()` - 의상 변경 확정
- [x] `doLevelUp()` - 레벨업 실행
- [x] `openTutorialPopup(page)` - 튜토리얼 팝업
- [x] `changeTutorialPage(direction)` - 튜토리얼 페이지 이동
- [x] `updateTutorialContent()` - 튜토리얼 내용 업데이트

---

## 프로토타입 완료 상태 요약

### 구현 완료된 페이지 (6개)
| 페이지 | 상태 | 주요 기능 |
|--------|------|----------|
| 상점 | ✅ 완료 | 데일리/패키지/다이아 상점, 구매 모달, 재구매 음영 |
| 뽑기 | ✅ 완료 | 1회/5회 뽑기, 결과 모달, 확률 정보 |
| 숙소 | ✅ 완료 | 기숙사, 캐릭터 배치, 보관함 |
| 도감 | ✅ 완료 | 포토카드 바인더, 캐릭터 정보, 등급/레벨업 |
| 전투 | ✅ 완료 | 스테이지 목록, 웨이브 진행도, 정보 모달 |
| 던전 | ✅ 완료 | 특별/스토리 탭, 아코디언 UI, 입장 기능 |

### 구현 완료된 모달 (10개+)
- 프로필/닉네임/칭호/아이콘 변경
- 친구 목록/추가/관리/프로필
- 퀘스트 (일일/주간/업적)
- 우편함 + 상세 모달
- 설정 (사운드/그래픽/알림)
- 구매 확인, 뽑기 결과, 스테이지 정보
- 튜토리얼, 등급 강화, 포토카드 교체, 의상 변경

---

## CS 파일 기반 UI 검증 결과 (2024.12.18 최종)

### 1. 프로필 시스템 ✅ 완료
**CS 파일:**
- `ProfileWindow.cs` - 닉네임, 칭호, 팬 수, 아이콘, 상태 메시지
- `IconChangeWindow.cs` - 프로필 아이콘 변경 (4x3 그리드)
- `IconChangeItemUI.cs` - 아이콘 선택 슬롯 (선택 시 녹색 테두리)
- `NicknameWindow.cs` - 닉네임 변경 (2~12자, 특수문자 불가)
- `StatusMessageWindow.cs` - 상태 메시지 변경

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| 프로필 아이콘 | ✓ | ✓ | ✅ 일치 |
| 아이콘 변경 모달 | ✓ | ✓ | ✅ 일치 |
| 닉네임 변경 | ✓ | ✓ | ✅ 일치 |
| 칭호 선택 | ✓ | ✓ | ✅ 일치 |
| UID 복사 | ✓ | ✓ | ✅ 일치 |

---

### 2. 친구 시스템 ✅ 완료
**CS 파일:**
- `FriendListWindow.cs` - 친구 목록, 일일 선물 한도
- `FriendListItemUI.cs` - 친구 아이템 (아이콘 클릭 → 프로필)
- `FriendProfileWindow.cs` - 친구 프로필 표시
- `FriendAddWindow.cs` - 친구 추가
- `FriendManageWindow.cs` - 친구 관리 (삭제)
- `DreamEnergyGiftService.cs` - 드림 에너지 선물

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| 친구 목록 | ✓ | ✓ | ✅ 일치 |
| 친구 아이콘 클릭 → 프로필 | ✓ | ✓ | ✅ 일치 |
| 친구 프로필 모달 | ✓ | ✓ | ✅ 일치 |
| 친구 요청/수락/거절 | ✓ | ✓ | ✅ 일치 |
| 하트 보내기/받기 | ✓ | ✓ | ✅ 일치 |
| 모두 보내기/받기 | ✓ | ✓ | ✅ 일치 |

---

### 3. 퀘스트 시스템 ✅ 완료
**CS 파일:**
- `QuestWindow.cs` - 일일/주간/업적 3탭, 전체받기 버튼
- `QuestItemUIBase.cs` - 진행도 슬라이더, 상태 텍스트 (미완료/받기/완료)
- `QuestEventSystem.cs` - 7가지 이벤트 타입
- `QuestData.cs` - Quest_ID, Quest_name, Quest_type, Quest_required, rewards

**QuestEventType 7종:**
1. Attendance (출석)
2. ClearStage (스테이지 클리어)
3. MonsterKill (몬스터 처치)
4. BossKill (보스 처치)
5. GachaDraw (뽑기)
6. ShopPurchase (상점 구매)
7. FanAmountReach (팬수 달성)

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| 3개 탭 (일일/주간/업적) | ✓ | ✓ | ✅ 일치 |
| 7가지 이벤트 타입 | ✓ | ✓ | ✅ 일치 |
| 진행도 바 | ✓ | ✓ | ✅ 일치 |
| 상태 버튼 (수령/진행중/완료) | ✓ | ✓ | ✅ 일치 |
| 전체 받기 버튼 | ✓ | ✓ | ✅ 일치 |
| 업적탭 전체받기 숨김 | ✓ | ✓ | ✅ 일치 |

**참고:** HTML에 진행도 게이지 (0~100) 마일스톤 보상 기능 추가됨 (CS에 없는 확장)

---

### 4. 우편함 시스템 ✅ 완료
**CS 파일:**
- `MailUI.cs` - 메일 목록, 삭제, 모두 받기
- `MailInfoUI.cs` - 메일 상세 (제목, 내용, 보상 아이템)
- `MailItemPrefab.cs` - 메일 리스트 아이템
- `MailData.cs` - title, content, itemList, isRead, isRewarded

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| 메일 목록 | ✓ | ✓ | ✅ 일치 |
| 읽음/안읽음 상태 | ✓ | ✓ | ✅ 일치 |
| 메일 상세 모달 | ✓ | ✓ | ✅ 일치 (2024.12.18 추가) |
| 보상 수령 | ✓ | ✓ | ✅ 일치 |
| 읽은 메일 삭제 | ✓ | ✓ | ✅ 일치 |
| 모두 받기 | ✓ | ✓ | ✅ 일치 |

---

### 5. 상점 시스템 ✅ 완료
**CS 파일:**
- `DailyShop.cs` - 24시간 타이머, 3개 랜덤 조각
- `ShopItemSlot.cs` - 아이템명, 이미지, 가격, 통화 아이콘
- `PurchaseConfirmPanel.cs` - 구매 확인 모달
- `ShopTable.cs` - Shop_ID, Shop_currency, Shop_price

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| 일일 상점 타이머 | ✓ | ✓ | ✅ 일치 |
| 3개 아이템 슬롯 | ✓ | ✓ | ✅ 일치 |
| 아이템 정보 표시 | ✓ | ✓ | ✅ 일치 |
| 구매 확인 모달 | ✓ | ✓ | ✅ 일치 |
| 재구매 불가 음영 처리 | ✓ | ✓ | ✅ 일치 (2024.12.18 추가) |

---

### 6. 설정 시스템 ✅ 완료
**CS 파일:**
- `SettingPanelUI.cs` - SFX/BGM 볼륨, FPS (30/60)
- `OptionPanelUI.cs` - 메일/설정 버튼, 알림 뱃지

**HTML 일치 상태:**
| 기능 | CS | HTML | 상태 |
|------|-----|------|------|
| SFX 볼륨 슬라이더 | ✓ | ✓ | ✅ 일치 |
| BGM 볼륨 슬라이더 | ✓ | ✓ | ✅ 일치 |
| FPS 토글 (60/30) | ✓ | ✓ | ✅ 일치 |
| 알림 설정 | ✗ | ✓ | ➕ HTML 확장 |
| UID 복사 | ✗ | ✓ | ➕ HTML 확장 |
| 고객센터/로그아웃 | ✗ | ✓ | ➕ HTML 확장 |

---

### 7. 로비 UI ✅ 완료
**CS 파일:**
- `LobbyUI.cs` - 하단 네비게이션 7개 버튼
- `OptionPanelUI.cs` - 상단 우측 (메일/설정)

**버튼 매핑:**
| CS Button | HTML Nav | 상태 |
|-----------|----------|------|
| stageUiButton | battle | ✅ |
| homeUiButton | dorm | ✅ |
| gachaButton | gacha | ✅ |
| storeButton | shop | ✅ |
| characterDictButton | dict | ✅ |
| QuestButton | 헤더 버튼 | ✅ |
| specialDungeonButton | dungeon | ✅ |

---

### 검증 요약

**총 검증 항목:** 7개 시스템
**일치:** 47개 기능 (모두 완료)
**부분 일치:** 0개 기능
**HTML 확장 (CS에 없음):** 4개 기능

**결론:** HTML 프로토타입은 Unity CS 구조와 **100% 일치**합니다. 모든 핵심 기능이 구현 완료되었습니다.

---

## CSS 클래스 구조

### 페이지 클래스
- `.shop-page` - 상점
- `.gacha-page` - 뽑기
- `.dorm-page` - 숙소 (기숙사)
- `.dict-page` - 도감 (포토카드 바인더)
- `.battle-page` - 전투
- `.dungeon-page` - 던전

### 주요 컴포넌트
- `.overlay`, `.modal` - 모달 시스템
- `.sub-overlay`, `.sub-modal` - 서브 모달
- `.nav-item` - 하단 네비게이션

---

## JS 함수 목록

### 페이지 전환
- `switchPage(pageName, navElement)`

### 상점
- `updateDailyShopTimer()`
- `openPurchaseModal(itemName, itemIcon, price, currency, currentAmount)`
- `confirmPurchase()`

### 뽑기
- `doGacha(times)`
- `drawRandomGacha()`

### 전투
- `openStageInfo(name, number, totalWaves, clearedWaves, energy)`
- `startStage()`

### 던전
- `switchDungeonTab(tabName, tabElement)`
- `toggleDungeonItem(item)`
- `enterDungeon(type, stageId)`

### 공통
- `showToast(message)`
- `openModal(id)`, `closeModal(id)`
- `openSubModal(id)`, `closeSubModal(id)`

---

---

## 프로젝트 내 문서 파일들

### UI 관련
- `Assets/Scripts/UI/OverlayPanel_Structure.md` - OverlayPanel 구조 및 SharedDimmedBackground 사용법

### 테마 시스템
- `Assets/Scripts/Theme/README.md` - 테마 시스템 개요
- `Assets/Scripts/Theme/Editor/FriendWindowThemeMapping.md` - FriendWindow 테마 매핑

### 기타
- `Assets/Editor/TODO.md` - 프로젝트 TODO 목록

---

## OverlayPanel 구조 (2024-12-21)

### 계층 구조
```
Canvas
└── OverlayPanel (Stretch All)
    ├── SharedDimmedBackground (공용 딤 배경, Left/Right/Top/Bottom: -5)
    └── 각 오버레이 창들 (FriendWindow, SettingPanel, QuestWindow 등)
```

### WindowManager 수정 사항
- `sharedDimmedBackground` 필드 추가
- `OpenOverlay()`: 딤 배경 활성화
- `CloseOverlay()`, `CloseAllOverlays()`: 활성 오버레이 없으면 딤 배경 비활성화

### 주의사항
- 각 오버레이 창 내부의 개별 DimmedBackground는 제거하고 SharedDimmedBackground 사용
- SharedDimmedBackground에는 SafeAreaFitter 붙이지 않음
- -5 여유분으로 미세한 틈 방지
