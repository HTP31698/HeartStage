using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class CharacterDropdownFilter : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown typeDropdown;
    [SerializeField] private TMP_Dropdown rankDropdown;
    [SerializeField] private TMP_Dropdown levelDropdown;

    [Header("Character UI")]
    [SerializeField] private RectTransform content;   // ScrollView → Content
    [SerializeField] private DragMe characterPrefab;  // DragMe 달려있는 프리팹

    // CSV 데이터 (전체)
    private List<CharacterCSVData> allCsvData = new List<CharacterCSVData>();

    // 로드된 SO 캐시 (AssetName → CharacterData)
    private readonly Dictionary<string, CharacterData> loadedSOCache = new Dictionary<string, CharacterData>();

    // 드롭다운 값 매핑용 (index -> 실제 값)
    private readonly List<int> _typeOptionValues = new List<int>();
    private readonly List<int> _rankOptionValues = new List<int>();
    private readonly List<int> _levelOptionValues = new List<int>();

    // 로딩 중 플래그
    private bool isLoading = false;

    private void Start()
    {
        // 자동 바인딩: content가 null이면 ScrollRect에서 찾기
        if (content == null)
        {
            var scrollRect = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (scrollRect != null)
                content = scrollRect.content;
        }

        // CSV 데이터 로드 (이미 메모리에 있음)
        var charTable = DataTableManager.Get<CharacterTable>(DataTableIds.Character);
        if (charTable != null)
        {
            allCsvData = charTable.GetAllCSV().ToList();
            Debug.Log($"[CharacterDropdownFilter] CSV 데이터 {allCsvData.Count}개 로드");
        }
        else
        {
            Debug.LogWarning("[CharacterDropdownFilter] CharacterTable이 없습니다.");
            return;
        }

        // 드롭다운 초기화
        InitDropdowns();

        // 첫 로드 (Rank1, Level1만)
        RefreshListAsync().Forget();
    }

    private void InitDropdowns()
    {
        InitTypeDropdown();
        InitRankDropdown();
        InitLevelDropdown();

        typeDropdown.onValueChanged.AddListener(_ => RefreshListAsync().Forget());
        rankDropdown.onValueChanged.AddListener(_ => RefreshListAsync().Forget());
        levelDropdown.onValueChanged.AddListener(_ => RefreshListAsync().Forget());
    }

    #region Dropdown Init

    private void InitTypeDropdown()
    {
        typeDropdown.ClearOptions();
        _typeOptionValues.Clear();

        var options = new List<TMP_Dropdown.OptionData>();

        // 0번: All
        options.Add(new TMP_Dropdown.OptionData("TypeAll"));
        _typeOptionValues.Add(-1);

        // 실제 타입 값들
        var distinctTypes = allCsvData
            .Select(c => c.char_type)
            .Distinct()
            .OrderBy(t => t);

        foreach (var t in distinctTypes)
        {
            string label = GetTypeName(t);
            options.Add(new TMP_Dropdown.OptionData(label));
            _typeOptionValues.Add(t);
        }

        typeDropdown.AddOptions(options);
        typeDropdown.value = 0;
        typeDropdown.RefreshShownValue();
    }

    private void InitRankDropdown()
    {
        rankDropdown.ClearOptions();
        _rankOptionValues.Clear();

        var options = new List<TMP_Dropdown.OptionData>();

        options.Add(new TMP_Dropdown.OptionData("RankAll"));
        _rankOptionValues.Add(-1);

        var distinctRanks = allCsvData
            .Select(c => c.char_rank)
            .Distinct()
            .OrderBy(r => r);

        foreach (var r in distinctRanks)
        {
            options.Add(new TMP_Dropdown.OptionData($"R{r}"));
            _rankOptionValues.Add(r);
        }

        rankDropdown.AddOptions(options);

        // 기본값: Rank 1 선택 (index 1 = R1)
        int defaultRankIndex = _rankOptionValues.IndexOf(1);
        rankDropdown.value = defaultRankIndex >= 0 ? defaultRankIndex : 0;
        rankDropdown.RefreshShownValue();
    }

    private void InitLevelDropdown()
    {
        levelDropdown.ClearOptions();
        _levelOptionValues.Clear();

        var options = new List<TMP_Dropdown.OptionData>();

        options.Add(new TMP_Dropdown.OptionData("LevelAll"));
        _levelOptionValues.Add(-1);

        var distinctLevels = allCsvData
            .Select(c => c.char_lv)
            .Distinct()
            .OrderBy(lv => lv);

        foreach (var lv in distinctLevels)
        {
            options.Add(new TMP_Dropdown.OptionData($"Lv.{lv}"));
            _levelOptionValues.Add(lv);
        }

        levelDropdown.AddOptions(options);

        // 기본값: Level 1 선택 (index 1 = Lv.1)
        int defaultLevelIndex = _levelOptionValues.IndexOf(1);
        levelDropdown.value = defaultLevelIndex >= 0 ? defaultLevelIndex : 0;
        levelDropdown.RefreshShownValue();
    }

    #endregion

    // 타입 숫자를 문자열로 바꿔주는 함수
    private string GetTypeName(int type)
    {
        return ((CharacterType)type).ToString();
    }

    // 현재 드롭다운 선택값 → 필터 조건
    private int? GetSelectedType()
    {
        int idx = typeDropdown.value;
        int raw = _typeOptionValues[idx];
        return raw == -1 ? (int?)null : raw;
    }

    private int? GetSelectedRank()
    {
        int idx = rankDropdown.value;
        int raw = _rankOptionValues[idx];
        return raw == -1 ? (int?)null : raw;
    }

    private int? GetSelectedLevel()
    {
        int idx = levelDropdown.value;
        int raw = _levelOptionValues[idx];
        return raw == -1 ? (int?)null : raw;
    }

    // 드롭다운 바뀔 때마다 호출 (비동기)
    private async UniTaskVoid RefreshListAsync()
    {
        if (isLoading) return;
        isLoading = true;

        try
        {
            int? typeFilter = GetSelectedType();
            int? rankFilter = GetSelectedRank();
            int? levelFilter = GetSelectedLevel();

            // 1. CSV에서 필터링
            IEnumerable<CharacterCSVData> query = allCsvData;

            if (typeFilter.HasValue)
                query = query.Where(c => c.char_type == typeFilter.Value);

            if (rankFilter.HasValue)
                query = query.Where(c => c.char_rank == rankFilter.Value);

            if (levelFilter.HasValue)
                query = query.Where(c => c.char_lv == levelFilter.Value);

            // 정렬: 이름 → 타입 → 랭크
            var filteredCsv = query
                .OrderBy(c => c.char_name)
                .ThenBy(c => c.char_type)
                .ThenBy(c => c.char_rank)
                .ToList();

            Debug.Log($"[CharacterDropdownFilter] 필터 결과: {filteredCsv.Count}개");

            // 2. 필요한 SO만 로드 (캐시 확인)
            var characterDataList = new List<CharacterData>();

            foreach (var csv in filteredCsv)
            {
                string assetName = csv.data_AssetName;

                if (loadedSOCache.TryGetValue(assetName, out var cached))
                {
                    characterDataList.Add(cached);
                }
                else
                {
                    // Addressables로 개별 로드
                    try
                    {
                        var so = await Addressables.LoadAssetAsync<CharacterData>(assetName);
                        if (so != null)
                        {
                            loadedSOCache[assetName] = so;
                            characterDataList.Add(so);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[CharacterDropdownFilter] SO 로드 실패: {assetName} - {e.Message}");
                    }
                }
            }

            // 3. UI 재생성
            RebuildCharacterUI(characterDataList);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void RebuildCharacterUI(List<CharacterData> list)
    {
        // 기존 슬롯 제거
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        // 새로 생성
        foreach (var characterData in list)
        {
            var dragMeInstance = Instantiate(characterPrefab, content);
            dragMeInstance.name = characterData.char_name;

            // DragMe 프리팹에 CharacterData 꽂기
            dragMeInstance.characterData = characterData;

            // CharacterSelectTestPanel 초기화
            var panel = dragMeInstance.GetComponent<CharacterSelectTestPanel>();
            if (panel != null)
            {
                panel.Init(characterData);
            }

            if (dragMeInstance.transform is RectTransform rect)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }
        }
    }
}
