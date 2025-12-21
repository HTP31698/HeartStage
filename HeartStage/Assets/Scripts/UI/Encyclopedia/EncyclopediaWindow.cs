using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EncyclopediaWindow : GenericWindow
{
    [Header("버튼 슬롯들")]
    public CharacterButtonView[] CharacterButtons;

    [Header("정렬 드롭다운 1개")]
    public TMP_Dropdown sortDropdown;

    [Header("상세 패널(공용 1개)")]
    public CharacterDetailPanel detailPanel;

    [Header("페이지네이션")]
    public Button leftButton;
    public Button rightButton;
    public TextMeshProUGUI pageText;

    private bool _initialized;
    private int _currentPage = 0;
    private int _totalPages = 1;
    private int _slotsPerPage;

    // 현재 보여줄 후보들/해금여부
    private List<CharacterCSVData> _candidates = new List<CharacterCSVData>();
    private List<bool> _unlockedList = new List<bool>();

    private void Start()
    {
        _slotsPerPage = CharacterButtons.Length;
        InitDropdown();
        InitPagination();
        RebuildAndRender();
        _initialized = true;
    }

    private void InitPagination()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(OnLeftButtonClick);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(OnRightButtonClick);
        }
    }

    private void OnLeftButtonClick()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RenderButtons();
            UpdatePaginationUI();
        }
    }

    private void OnRightButtonClick()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RenderButtons();
            UpdatePaginationUI();
        }
    }

    private void UpdatePaginationUI()
    {
        // 페이지 텍스트 업데이트
        if (pageText != null)
            pageText.text = $"{_currentPage + 1} / {_totalPages}";

        // 버튼 활성화/비활성화
        if (leftButton != null)
            leftButton.interactable = _currentPage > 0;

        if (rightButton != null)
            rightButton.interactable = _currentPage < _totalPages - 1;
    }

    private void OnEnable()
    {
        if (_initialized)
        {
            _currentPage = 0; // 창 열 때마다 첫 페이지로
            RebuildAndRender();
        }
    }

    private void InitDropdown()
    {
        if (sortDropdown == null) return;

        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string>
        {
            "등급순",
            "레벨순",
            "이름순",
            "속성순"
        });

        sortDropdown.onValueChanged.RemoveAllListeners();
        sortDropdown.onValueChanged.AddListener(_ =>
        {
            _currentPage = 0; // 정렬 변경 시 첫 페이지로
            RebuildAndRender();
        });
    }

    private void RebuildAndRender()
    {
        BuildCandidates();
        ApplySort(_candidates, _unlockedList, sortDropdown != null ? sortDropdown.value : 0);

        // 총 페이지 수 계산
        _totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_candidates.Count / _slotsPerPage));

        // 현재 페이지가 범위를 벗어나면 조정
        _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);

        RenderButtons();
        UpdatePaginationUI();
    }

    // ✅ SaveLoadManager.unlockedByName 키만 후보로
    private void BuildCandidates()
    {
        _candidates.Clear();
        _unlockedList.Clear();

        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        var unlockedByName = saveData.unlockedByName;
        if (unlockedByName == null) return;

        foreach (var kvp in unlockedByName)
        {
            string name = kvp.Key;
            bool unlocked = kvp.Value;

            CharacterCSVData data = null;


            if (unlocked)
            {
                // 🔹 현재 내가 실제로 들고 있는 이 이름의 캐릭 ID를 ownedIds에서 찾기
                int id = FindCurrentIdByName(name);
                if (id > 0)
                {
                    data = DataTableManager.CharacterTable.Get(id);
                }

                // 혹시 못 찾으면(버그나 데이터 꼬임) 기본값으로 폴백
                if (data == null)
                {
                    data = DataTableManager.CharacterTable.GetByName(name);
                }
            }
            else
            {
                // 잠금 상태면 그냥 기본 row(보통 1렙)만 보여줌
                data = DataTableManager.CharacterTable.GetByName(name);
            }

            if (data == null) continue;

            _candidates.Add(data);
            _unlockedList.Add(unlocked);
        }
    }

    // 🔸 SaveData.ownedIds에서 이름으로 현재 ID 찾기
    private int FindCurrentIdByName(string name)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return -1;

        foreach (var id in saveData.ownedIds)
        {
            var row = DataTableManager.CharacterTable.Get(id);
            if (row == null) continue;

            if (row.char_name == name)
                return id;
        }

        return -1;
    }


    private void RenderButtons()
    {
        int startIndex = _currentPage * _slotsPerPage;

        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            int dataIndex = startIndex + i;

            if (dataIndex < _candidates.Count)
            {
                var data = _candidates[dataIndex];
                bool unlocked = _unlockedList[dataIndex];
                var btnView = CharacterButtons[i];

                btnView.gameObject.SetActive(true);
                btnView.SetButton(data.char_id);
                btnView.SetLocked(!unlocked);

                // ✅ 이름으로 묶기
                BindClick(btnView, data.char_name);
            }
            else
            {
                CharacterButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void BindClick(CharacterButtonView btnView, string charName)
    {
        var uiButton = btnView.GetComponent<Button>();
        if (uiButton == null) return;

        uiButton.onClick.RemoveAllListeners();
        uiButton.onClick.AddListener(() => OnCharacterSelectedByName(charName));
    }

    private void OnCharacterSelectedByName(string name)
    {
        if (detailPanel == null)
        {
            Debug.LogWarning("[EncyclopediaWindow] detailPanel null");
            return;
        }

        // 🔹 항상 SaveData.ownedIds에서 "지금 갖고 있는" 최신 ID를 찾음
        int id = FindCurrentIdByName(name);
        CharacterCSVData csvdata = null;

        if (id > 0)
            csvdata = DataTableManager.CharacterTable.Get(id);

        // 못 찾으면 기본값 fallback
        if (csvdata == null)
            csvdata = DataTableManager.CharacterTable.GetByName(name);

        if (csvdata == null)
        {
            Debug.LogWarning($"[EncyclopediaWindow] CharacterData null by name: {name}");
            return;
        }

        detailPanel.SetCharacter(csvdata);
        detailPanel.OpenPanel();
    }

    // true 먼저 / false 뒤로 + 내부는 드롭다운 정렬
    private void ApplySort(List<CharacterCSVData> list, List<bool> unlockedList, int sortIndex)
    {
        // 인덱스 배열 생성 및 정렬 (O(n log n))
        int[] indices = new int[list.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        System.Array.Sort(indices, (a, b) =>
            CompareWithUnlocked(list[a], unlockedList[a], list[b], unlockedList[b], sortIndex));

        // 정렬된 순서로 새 리스트 생성
        var sortedData = new List<CharacterCSVData>(list.Count);
        var sortedUnlocked = new List<bool>(list.Count);
        for (int i = 0; i < indices.Length; i++)
        {
            sortedData.Add(list[indices[i]]);
            sortedUnlocked.Add(unlockedList[indices[i]]);
        }

        // 원본 리스트에 복사
        list.Clear();
        list.AddRange(sortedData);
        unlockedList.Clear();
        unlockedList.AddRange(sortedUnlocked);
    }

    private int CompareWithUnlocked(CharacterCSVData A, bool unlockedA,
                                    CharacterCSVData B, bool unlockedB,
                                    int sortIndex)
    {
        // 1순위: unlocked true 먼저
        if (unlockedA != unlockedB)
            return unlockedB.CompareTo(unlockedA);

        // 2순위: 드롭다운 기준
        return CompareData(A, B, sortIndex);
    }

    private int CompareData(CharacterCSVData A, CharacterCSVData B, int sortIndex)
    {
        string nameA = A.char_name ?? "";
        string nameB = B.char_name ?? "";

        switch (sortIndex)
        {
            case 0: // 등급순 (높은 등급 먼저)
                {
                    int c = B.char_rank.CompareTo(A.char_rank);  // ← A, B 순서 바꿈
                    if (c != 0) return c;
                    c = B.char_lv.CompareTo(A.char_lv);          // ← A, B 순서 바꿈
                    if (c != 0) return c;
                    return string.CompareOrdinal(nameA, nameB);
                }
            case 1: // 레벨순 (높은 레벨 먼저)
                {
                    int c = B.char_lv.CompareTo(A.char_lv);      // ← A, B 순서 바꿈
                    if (c != 0) return c;
                    c = B.char_rank.CompareTo(A.char_rank);      // ← A, B 순서 바꿈
                    if (c != 0) return c;
                    return string.CompareOrdinal(nameA, nameB);
                }
            case 2: // 이름순 (가나다순 - 이건 그대로)
                return string.CompareOrdinal(nameA, nameB);

            case 3: // 속성순
                {
                    int c = A.char_type.CompareTo(B.char_type);  // 속성은 보통 오름차순
                    if (c != 0) return c;
                    c = B.char_rank.CompareTo(A.char_rank);      // ← 등급은 내림차순
                    if (c != 0) return c;
                    c = B.char_lv.CompareTo(A.char_lv);          // ← 레벨도 내림차순
                    if (c != 0) return c;
                    return string.CompareOrdinal(nameA, nameB);
                }
        }
        return 0;
    }
}
