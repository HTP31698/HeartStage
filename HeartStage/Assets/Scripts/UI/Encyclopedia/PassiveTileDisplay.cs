using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패시브 타일 패턴을 5x3 그리드로 시각화하는 컴포넌트
/// 중앙(2,1)이 캐릭터 위치, 주변이 버프 영역
/// 가로 5칸 x 세로 3칸 = 15칸
/// </summary>
public class PassiveTileDisplay : MonoBehaviour
{
    [Header("타일 이미지 (5x3 = 15개)")]
    [Tooltip("좌상단부터 우하단까지 순서: 행 우선 (가로 5개씩 3줄)")]
    [SerializeField] private Image[] tileImages = new Image[15];

    [Header("타일 색상")]
    [SerializeField] private Color activeTileColor = new Color(1f, 0.8f, 0.2f, 1f);  // 활성 타일 (노란색)
    [SerializeField] private Color inactiveTileColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // 비활성 타일
    [SerializeField] private Color centerTileColor = new Color(0.2f, 0.6f, 1f, 1f);  // 중앙 타일 (파란색, 캐릭터 위치)

    [Header("패턴 데이터")]
    [SerializeField] private PassivePatternData patternData;

    // 그리드 크기
    private const int GridWidth = 5;
    private const int GridHeight = 3;
    private const int CenterX = 2;  // 중앙 X (0,1,2,3,4 중 2)
    private const int CenterY = 1;  // 중앙 Y (0,1,2 중 1)

    private int _currentPatternId = -1;

    /// <summary>
    /// passive_type ID로 패턴 설정
    /// </summary>
    public void SetPattern(int passiveTypeId)
    {
        if (_currentPatternId == passiveTypeId)
            return;

        _currentPatternId = passiveTypeId;

        // 모든 타일 비활성 색상으로 초기화
        ResetAllTiles();

        // 중앙 타일 (캐릭터 위치) 항상 표시
        SetTileColor(CenterX, CenterY, centerTileColor);

        // 패턴이 없으면 중앙만 표시
        if (passiveTypeId == 0 || patternData == null)
            return;

        // 패턴 오프셋 가져오기
        Vector2Int[] offsets = patternData.GetPattern(passiveTypeId);
        if (offsets == null || offsets.Length == 0)
            return;

        // 각 오프셋에 해당하는 타일 활성화
        foreach (var offset in offsets)
        {
            // 오프셋은 중앙(0,0) 기준이므로 그리드 좌표로 변환
            // X: -2~2 -> 0~4
            // Y: -1~1 -> 0~2 (y축 반전: 위가 -, 아래가 +)
            int gridX = CenterX + offset.x;
            int gridY = CenterY - offset.y;  // y축 반전

            // 범위 체크
            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                continue;

            // 중앙은 이미 설정됨
            if (gridX == CenterX && gridY == CenterY)
                continue;

            SetTileColor(gridX, gridY, activeTileColor);
        }
    }

    /// <summary>
    /// 모든 타일을 비활성 색상으로 리셋
    /// </summary>
    private void ResetAllTiles()
    {
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                SetTileColor(x, y, inactiveTileColor);
            }
        }
    }

    /// <summary>
    /// 특정 그리드 위치의 타일 색상 설정
    /// </summary>
    private void SetTileColor(int x, int y, Color color)
    {
        int index = y * GridWidth + x;
        if (index < 0 || index >= tileImages.Length)
            return;

        if (tileImages[index] != null)
            tileImages[index].color = color;
    }

    /// <summary>
    /// 패턴 초기화 (모두 비활성)
    /// </summary>
    public void Clear()
    {
        _currentPatternId = -1;
        ResetAllTiles();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 미리보기용
    /// </summary>
    [ContextMenu("Preview Pattern 1")]
    private void PreviewPattern1() => SetPattern(1);

    [ContextMenu("Preview Pattern 2")]
    private void PreviewPattern2() => SetPattern(2);

    [ContextMenu("Clear Preview")]
    private void ClearPreview() => Clear();
#endif
}
