# OverlayPanel 구조 문서

## 개요
태블릿/폴더블 등 다양한 화면 비율 대응을 위한 오버레이 UI 구조 리팩토링

---

## Canvas 계층 구조

```
Canvas (Scale With Screen Size, 1080x1920, Match=0.5)
├── LobbyUI
│   ├── Top
│   ├── Middle (메인 컨텐츠 영역)
│   └── Bottom
│
└── OverlayPanel (Stretch All, offset 0)
    ├── SharedDimmedBackground (공용 딤 배경)
    │   - RectTransform: Stretch All, Left/Right/Top/Bottom: -5 (여유분)
    │   - Image: Color (0,0,0,0.5), raycastTarget: true
    │   - 초기 상태: 비활성화
    │
    └── 각 오버레이 창들
        ├── FriendWindow
        ├── SettingPanel
        ├── QuestWindow
        ├── MailUI
        ├── EncyclopediaWindow
        ├── ShoppingWindow
        ├── SpecialDungeonWindow
        ├── StoryDungeonUI
        ├── FriendProfileWindow
        └── ... (기타 오버레이)
```

---

## WindowManager 수정 사항

### 추가된 필드
```csharp
[Header("공용 딤 배경")]
[SerializeField] private GameObject sharedDimmedBackground;
```

### OpenOverlay() 수정
```csharp
public void OpenOverlay(WindowType id)
{
    if (!IsValidWindow(id)) return;
    if (windows[id].gameObject.activeSelf) return;

    // 공용 딤 배경 활성화
    if (sharedDimmedBackground != null)
    {
        sharedDimmedBackground.SetActive(true);
    }

    windows[id].Open();

    if (!activeOverlays.Contains(id))
    {
        activeOverlays.Add(id);
    }
}
```

### CloseOverlay() 수정
```csharp
public void CloseOverlay(WindowType id)
{
    if (!IsValidWindow(id)) return;

    windows[id].Close();
    activeOverlays.Remove(id);

    // 활성 오버레이가 없으면 공용 딤 배경 비활성화
    if (activeOverlays.Count == 0 && sharedDimmedBackground != null)
    {
        sharedDimmedBackground.SetActive(false);
    }
}
```

### CloseAllOverlays() 수정
```csharp
public void CloseAllOverlays()
{
    for (int i = activeOverlays.Count - 1; i >= 0; i--)
    {
        // ... 기존 로직 ...
    }

    // 공용 딤 배경 비활성화
    if (sharedDimmedBackground != null)
    {
        sharedDimmedBackground.SetActive(false);
    }
}
```

---

## Unity Inspector 설정

### LobbyManager (WindowManager 컴포넌트)
1. `sharedDimmedBackground` 필드에 SharedDimmedBackground 오브젝트 연결
2. `windowList`에서 각 WindowType에 해당 창 연결 확인
   - WindowType.Friend (30) → FriendWindow

### SharedDimmedBackground
- **RectTransform**: Anchor Stretch All, Left/Right/Top/Bottom: -5
- **Image**: Color rgba(0,0,0,0.5), Raycast Target: true
- **초기 상태**: 비활성화 (SetActive: false)

---

## WindowType 목록 (Defines.cs)

| Value | Name | 설명 |
|-------|------|------|
| 0 | LobbyHome | 로비 홈 |
| 1 | StageSelect | 스테이지 선택 |
| 2 | StageInfo | 스테이지 정보 |
| 3 | Gacha | 가챠 |
| 4 | GachaPercentage | 가챠 확률 |
| 5 | GachaResult | 가챠 결과 |
| 6 | Gacha5TryResult | 5연차 결과 |
| 7 | Quest | 퀘스트 |
| 8 | GachaCancel | 가챠 취소 |
| 9 | MonitoringCharacterSelect | 모니터링 캐릭터 선택 |
| 10 | MonitoringReward | 모니터링 보상 |
| 11 | MailUI | 메일 |
| 12 | MailInfoUI | 메일 상세 |
| 13 | SettingPanel | 설정 |
| 14 | Shopping | 상점 |
| 15 | CharacterDict | 캐릭터 도감 |
| 16 | SpecialDungeon | 특수 던전 |
| 17 | StoryDungeon | 스토리 던전 |
| 18 | StoryDungeonInfo | 스토리 던전 정보 |
| 19 | SpecialStage | 특수 스테이지 |
| 20 | StoryStageRewardUI | 스토리 보상 |
| 30 | Friend | 친구 창 (통합) |
| 33 | FriendProfile | 친구 프로필 |

---

## 주의사항

1. **개별 딤 배경 제거**: 각 오버레이 창 내부의 개별 DimmedBackground는 제거하고 SharedDimmedBackground 사용
2. **SafeAreaFitter**: SharedDimmedBackground에는 SafeAreaFitter 붙이지 않음 (전체 화면 덮어야 함)
3. **-5 여유분**: 모바일 기기에서 미세한 틈 방지를 위해 Left/Right/Top/Bottom에 -5 설정

---

## 파일 위치
- WindowManager: `Assets/Scripts/WindowManager.cs`
- Defines: `Assets/Scripts/Defines.cs`
- GenericWindow: `Assets/Scripts/GenericWindow.cs`
