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
