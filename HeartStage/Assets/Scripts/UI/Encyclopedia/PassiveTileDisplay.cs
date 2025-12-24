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

    [Header("중앙 캐릭터 이미지 (별도)")]
    [SerializeField] private Image centerCharacterImage;

    [Header("타일 색상")]
    [SerializeField] private Color inactiveTileColor = new Color(0.75f, 0.75f, 0.75f, 1f);  // 비활성 타일 (밝은 gray)

    // 캐릭터 타입별 활성 타일 색상 (CharacterAttribute 순서: 1=보컬, 2=랩, 3=카리스마, 4=큐티, 5=댄스, 6=비주얼, 7=섹시)
    private static readonly Color[] TypeColors = new Color[]
    {
        new Color(0.75f, 0.75f, 0.75f, 1f),       // 0: 기본 (사용 안함)
        new Color(1f, 0.85f, 0.3f, 1f),           // 1: 보컬 - 노란색/오렌지
        new Color(1f, 0.4f, 0.6f, 1f),            // 2: 랩 - 핫핑크
        new Color(1f, 0.5f, 0.2f, 1f),            // 3: 카리스마 - 주황
        new Color(0.9f, 0.7f, 0.95f, 1f),         // 4: 큐티 - 연보라/라이트핑크
        new Color(0.4f, 0.85f, 0.5f, 1f),         // 5: 댄스 - 초록
        new Color(0.4f, 0.75f, 1f, 1f),           // 6: 비주얼 - 하늘색
        new Color(0.7f, 0.3f, 0.9f, 1f),          // 7: 섹시 - 보라
    };

    private int _currentCharType = 0;

    [Header("패턴 데이터")]
    [SerializeField] private PassivePatternData patternData;

    // 그리드 크기
    private const int GridWidth = 5;
    private const int GridHeight = 3;
    private const int CenterX = 2;  // 중앙 X (0,1,2,3,4 중 2)
    private const int CenterY = 1;  // 중앙 Y (0,1,2 중 1)

    private int _currentPatternId = -1;

    /// <summary>
    /// passive_type ID로 패턴 설정 (캐릭터 타입에 따라 색상 적용)
    /// </summary>
    public void SetPattern(int passiveTypeId, int charType = 0)
    {
        // 캐릭터 타입이나 패턴이 바뀌면 다시 그리기
        if (_currentPatternId == passiveTypeId && _currentCharType == charType)
            return;

        _currentPatternId = passiveTypeId;
        _currentCharType = charType;

        // 모든 타일 비활성 색상으로 초기화
        ResetAllTiles();

        // 중앙 캐릭터 이미지 색상 설정
        if (centerCharacterImage != null)
            centerCharacterImage.color = GetTypeColor(charType);

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

            // 캐릭터 타입에 따른 색상 선택
            Color activeColor = GetTypeColor(charType);
            SetTileColor(gridX, gridY, activeColor);
        }
    }

    /// <summary>
    /// 캐릭터 타입에 따른 활성 타일 색상 반환
    /// </summary>
    private Color GetTypeColor(int charType)
    {
        if (charType >= 0 && charType < TypeColors.Length)
            return TypeColors[charType];
        return TypeColors[0];
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
