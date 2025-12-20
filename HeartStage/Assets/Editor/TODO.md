# Editor Tools TODO

## SOBalancingWindow

### 예정된 작업

#### 비교 뷰 분리 (SOCompareWindow)
- [ ] SOBalancingWindow에서 비교 뷰 관련 코드 제거
  - `ViewMode.Compare`
  - `compareIndex`
  - `DrawCompareView()`
  - `DrawCompareList()`
  - 관련 스크롤 변수 등
- [ ] 새 에디터 윈도우 생성: `SOCompareWindow.cs`
- [ ] 기능 구현 예정:
  - 다중 선택 비교 (3개 이상 SO 동시 비교)
  - Diff 하이라이트 (변경된 필드 색상 표시)
  - 변경 이력 추적
  - 비교 결과 내보내기
- [ ] SOCompareWindow는 자체적으로 SO 데이터를 로드하여 비교

---

## 완료된 작업

### 2024-12
- [x] 지연 저장 패턴 구현 (pendingChanges)
- [x] CSV 가져오기/내보내기 정렬 일관성 유지
- [x] Ctrl+클릭으로 Inspector 열기
- [x] 사용되지 않는 ref 버전 메서드 삭제
- [x] filterCharRank UI 추가
- [x] 색상 테마 적용 (섹션 헤더)
- [x] 바로가기 버튼 추가 (웨이브, 스킬, 몬스터, 캐릭터)

---

## UI 시스템

### ToastUI (2025-12)
범용 토스트 알림 시스템 구현 완료

**파일:**
- `Scripts/UI/Common/ToastUI.cs` - 메인 싱글톤
- `Scripts/UI/Common/ToastItemView.cs` - 개별 토스트 뷰
- `Prefabs/UI/ToastCanvas.prefab` - 캔버스 프리팹
- `Prefabs/UI/ToastItem.prefab` - 토스트 아이템 프리팹

**특징:**
- DontDestroyOnLoad (씬 전환 시 유지)
- 동시 표시 1개 (새 메시지 오면 교체)
- 슬라이드 다운 + 페이드 애니메이션 (DOTween)
- 다크 그레이 배경 `rgba(0.12, 0.12, 0.12, 0.8)`
- RaycastTarget = false (터치 안 막음)
- 2줄 텍스트 지원 (예: "정말대단한토스트유아이탄생\n정말대단한토스트유아이탄생")

**사용법:**
```csharp
ToastUI.Show("메시지");
ToastUI.Show("메시지", 3f);  // 3초 표시
ToastUI.Warning("재화가 부족합니다");  // 하위 호환
```

**프리팹 연결 (수동):**
1. ToastItem 프리팹:
   - ToastItemView.backgroundImage → Image
   - ToastItemView.messageText → MessageText (TMP)
   - ToastItemView.canvasGroup → CanvasGroup
2. ToastCanvas 프리팹:
   - ToastUI.toastItemPrefab → ToastItem.prefab
   - ToastUI.toastContainer → ToastContainer (또는 자기 자신)

### NoteLoadingUI (2025-12)
- [x] DontDestroyOnLoad 추가
