# ExpandablePanel 사용 가이드

## 개요
`ExpandablePanel`은 아코디언 스타일의 확장/축소 애니메이션을 제공하는 범용 컴포넌트입니다.
헤더 클릭 시 콘텐츠가 부드럽게 슬라이드되며, 아래 항목들이 자연스럽게 밀려납니다.

## 적용 대상
- 공지사항 (NoticeItemUI) ✅ 적용됨
- 스토리 던전 (StoryChapterPrefab) ✅ 적용됨
- 메일 (선택적 - 현재는 별도 오버레이 방식)
- 무한/극한 던전 (필요 시 적용 가능)

---

## 사용법

### 방법 1: 프리팹에 직접 추가

```
프리팹 구조:
├── ExpandablePrefab (ExpandablePanel 컴포넌트)
│   ├── Header (Button) ← headerButton에 할당
│   │   ├── Title
│   │   └── ArrowIcon ← arrowIcon에 할당 (선택)
│   └── Content (RectTransform) ← contentRect에 할당
│       ├── Item 1
│       ├── Item 2
│       └── Item 3
```

1. 프리팹 루트에 `ExpandablePanel` 컴포넌트 추가
2. Inspector에서 설정:
   - `Header Button`: 클릭할 버튼
   - `Content Rect`: 펼쳐질 영역
   - `Arrow Icon`: 화살표 아이콘 (선택, 펼침 시 회전)
   - `Start Expanded`: 초기 펼침 상태
   - `Use Fade`: 페이드 효과 사용
   - `Accordion Group Id`: 그룹 ID (같은 그룹은 하나만 펼침)

### 방법 2: 기존 컴포넌트에 통합 (NoticeItemUI 예시)

```csharp
public class NoticeItemUI : MonoBehaviour
{
    [SerializeField] private ExpandablePanel expandablePanel;

    private void Awake()
    {
        if (expandablePanel == null)
            expandablePanel = GetComponent<ExpandablePanel>();
    }
}
```

---

## Inspector 설정

| 속성 | 설명 | 기본값 |
|------|------|--------|
| `headerButton` | 클릭하면 토글되는 헤더 버튼 | - |
| `contentRect` | 펼쳐지는 콘텐츠 영역 | - |
| `contentCanvasGroup` | 페이드용 CanvasGroup (자동 생성됨) | - |
| `arrowIcon` | 화살표 아이콘 (회전 애니메이션) | - |
| `expandDuration` | 펼침 애니메이션 시간 | 0.25s |
| `collapseDuration` | 접힘 애니메이션 시간 | 0.2s |
| `expandEase` | 펼침 이징 | OutCubic |
| `collapseEase` | 접힘 이징 | InCubic |
| `arrowRotationExpanded` | 펼침 시 화살표 각도 | 180° |
| `arrowRotationCollapsed` | 접힘 시 화살표 각도 | 0° |
| `startExpanded` | 시작 시 펼침 상태 | false |
| `useFade` | 페이드 효과 사용 | true |
| `usePreferredHeight` | LayoutElement 사용 | true |
| `accordionGroupId` | 그룹 ID (0=그룹 없음) | 0 |

---

## API

### 기본 메서드

```csharp
// 토글
expandablePanel.Toggle();

// 펼치기
expandablePanel.Expand();

// 접기
expandablePanel.Collapse();

// 애니메이션 없이 즉시 설정
expandablePanel.SetExpandedImmediate(true);

// 동적 콘텐츠 높이 재계산
expandablePanel.RefreshExpandedHeight();
```

### 런타임 설정

```csharp
// 헤더 버튼 변경
expandablePanel.SetHeaderButton(newButton);

// 콘텐츠 영역 변경
expandablePanel.SetContentRect(newRect);

// 아코디언 그룹 설정
expandablePanel.SetAccordionGroup(1);
```

### 이벤트

```csharp
expandablePanel.OnExpandChanged += (isExpanded) => {
    Debug.Log($"펼침 상태: {isExpanded}");
};
```

### 프로퍼티

```csharp
bool isExpanded = expandablePanel.IsExpanded;
float height = expandablePanel.GetExpandedHeight();
float headerHeight = expandablePanel.GetHeaderHeight();
RectTransform content = expandablePanel.ContentRect;
```

---

## 아코디언 그룹

같은 `accordionGroupId`를 가진 패널들은 하나만 펼쳐집니다:

```
ScrollView Content
├── Panel A (groupId: 1) ← 펼쳐짐
├── Panel B (groupId: 1) ← A 펼치면 자동으로 접힘
└── Panel C (groupId: 1) ← A 펼치면 자동으로 접힘
```

**런타임 그룹 설정:**
```csharp
foreach (var panel in panels)
{
    panel.SetAccordionGroup(1);
}
```

---

## 레이아웃 요구사항

### 부모 요소

```
ScrollView Content
├── VerticalLayoutGroup (필수)
├── ContentSizeFitter (Vertical: Preferred Size)
└── Child Force Expand Width: true
```

### ExpandablePanel 프리팹

```
ExpandablePrefab
├── LayoutElement (자동 추가됨)
├── Header (고정 높이)
└── Content
    ├── VerticalLayoutGroup
    └── ContentSizeFitter (동적 콘텐츠용)
```

---

## 동적 콘텐츠

런타임에 콘텐츠가 변경될 때:

```csharp
// 콘텐츠 생성 후 높이 재계산
CreateChildItems();
expandablePanel.RefreshExpandedHeight();
```

**StoryChapterPrefab 예시:**
```csharp
public void Initialize(string characterId, List<StoryStageCSVData> stages)
{
    // 스테이지 프리팹 생성
    foreach (var stage in stages)
    {
        Instantiate(stagePrefab, stagesContent);
    }

    // 높이 재계산
    expandablePanel.RefreshExpandedHeight();
}
```

---

## 적용 예시

### NoticeItemUI

```csharp
// 프리팹에 ExpandablePanel 컴포넌트 추가
// Inspector에서:
// - headerButton: 공지 헤더 버튼
// - contentRect: bodyRoot (본문 영역)
// - arrowIcon: 화살표 이미지
```

### StoryChapterPrefab

```csharp
// 프리팹 구조:
// StoryChapterPrefab (ExpandablePanel)
// ├── Header (캐릭터 이미지, 챕터명, 진행도)
// └── StagesContent (스테이지 목록)

[SerializeField] private ExpandablePanel expandablePanel;

public void Initialize(...)
{
    CreateStagePrefabs();
    expandablePanel.RefreshExpandedHeight();
}
```

---

## 문제 해결

### 애니메이션이 안 나옴
- `contentRect`가 올바르게 할당되었는지 확인
- 부모에 `VerticalLayoutGroup`이 있는지 확인

### 높이가 이상함
- `RefreshExpandedHeight()` 호출
- 콘텐츠에 `ContentSizeFitter` 추가

### 레이아웃이 업데이트 안됨
- 부모에 `ContentSizeFitter (Vertical: Preferred Size)` 추가
- `VerticalLayoutGroup`의 Child Control Size 확인

### 그룹 작동 안함
- `accordionGroupId`가 1 이상인지 확인 (0은 그룹 없음)
