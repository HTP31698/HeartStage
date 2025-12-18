# Lobby Animation System 작업 현황

## 현재 상태 (2024-12-18)

### 완료된 작업
1. **WindowType enum 정리** - 슬라이드 방향용 순서 설정
2. **애니메이션 시스템 코드 작성 완료**
3. **에디터 도구 작성 완료**

### 생성된 파일들

#### 1. LobbyAnimations.cs
- 경로: `Assets/Scripts/Lobby/LobbyAnimations.cs`
- DOTween 기반 애니메이션 함수들
- ScaleIn/Out, SlideUp/Down/Left/Right, FadeIn/Out
- PageSlideInFromRight/Left, PageSlideOutToLeft/Right

#### 2. WindowAnimationTrigger.cs
- 경로: `Assets/Scripts/Lobby/WindowAnimationTrigger.cs`
- GenericWindow에 붙이는 컴포넌트
- AnimType: None, ScaleIn, SlideUp, SlideDown, SlideLeft, SlideRight, FadeIn, PageSlideFromRight, PageSlideFromLeft
- SetDirection(bool) - 동적 슬라이드 방향 설정

#### 3. LobbyAnimationSetup.cs (에디터)
- 경로: `Assets/Editor/LobbyAnimationSetup.cs`
- Tools > HeartStage 메뉴:
  - Setup Lobby Animations - 애니메이션 트리거 자동 추가
  - Remove All Animation Triggers - 트리거 제거
  - Setup WindowManager - windowList 자동 설정
  - Validate WindowManager - 설정 검증
  - Reset All (Clear WindowManager) - 전체 초기화

---

## WindowType 순서 (메인 네비게이션)

```csharp
LobbyHome = 0,      // 홈
Shopping = 1,       // 상점
Gacha = 2,          // 뽑기
Dorm = 3,           // 숙소 (NEW)
CharacterDict = 4,  // 캐릭터 도감
StageSelect = 5,    // 전투
SpecialDungeon = 6, // 던전
```

**슬라이드 방향 로직**: 인덱스가 큰 쪽으로 이동하면 오른쪽에서 슬라이드, 작은 쪽이면 왼쪽에서 슬라이드

---

## 애니메이션 매핑

| 윈도우 클래스 | Open 애니메이션 | Close 애니메이션 |
|--------------|----------------|-----------------|
| LobbyHome | FadeIn | FadeIn |
| ShoppingWindow | PageSlide | PageSlide |
| GachaUI | PageSlide | PageSlide |
| DormWindow | PageSlide | PageSlide |
| EncyclopediaWindow | PageSlide | PageSlide |
| StageSelect/StageWindow | PageSlide | PageSlide |
| SpecialDungeonUI | PageSlide | PageSlide |
| QuestWindow | ScaleIn | ScaleIn |
| MailUI | ScaleIn | ScaleIn |
| SettingPanel | ScaleIn | ScaleIn |
| StageInfoWindow | SlideUp | SlideUp |
| GachaResultUI | ScaleIn | ScaleIn |

---

## 다음 작업 (MCP 연동 후)

### 1. HTML 프로토타입 참조
- 경로: `Tools/LobbyPanelPrototype_v3.html`
- 이 파일의 애니메이션 스타일 참고

### 2. MCP로 씬 읽기
- Unity MCP 서버 연결
- Lobby 씬의 현재 윈도우 구조 확인
- WindowManager.windowList 자동 설정

### 3. 작업 순서
1. MCP 연결 확인
2. Lobby 씬 열기
3. `Tools > HeartStage > Setup Lobby Animations` 실행
4. `Tools > HeartStage > Setup WindowManager` 실행
5. 씬 저장 (Ctrl+S)
6. 플레이 테스트

---

## 수정된 기존 파일들

### GenericWindow.cs
- WindowAnimationTrigger 연동 추가
- Open()에서 PlayOpenAnimation() 호출
- Close()에서 PlayCloseAnimation() 호출 후 비활성화

### WindowManager.cs
- HeartStage.UI 네임스페이스 using 추가
- SetSlideDirection() 메서드 추가
- Open()에서 슬라이드 방향 자동 계산

### Defines.cs
- WindowType enum 순서 재정렬
- Dorm 타입 추가
- 서브 윈도우 인덱스 20번대로 이동

---

## 주의사항
- 숙소(Dorm) 윈도우 클래스 `DormWindow.cs`가 아직 없음 - 필요시 생성
- Unity 씬의 WindowManager.windowList는 enum 값 변경으로 인해 재설정 필요
- MCP 연결 안되면 Unity에서 직접 메뉴 실행해야 함
