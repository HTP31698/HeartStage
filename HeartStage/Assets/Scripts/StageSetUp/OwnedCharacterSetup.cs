using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class OwnedCharacterSetup : MonoBehaviour
{
    [Header("UI 부모 (Scroll View의 Content)")]
    public RectTransform content;
    public DragMe characterPrefab;

    private readonly List<CharacterData> _ownedCharacters = new List<CharacterData>();

    public bool IsReady { get; private set; }

    // 🔹 이 컴포넌트가 담당할 전역 프로그레스 구간 (60% ~ 85%)
    private const float GlobalStart = 0.6f;
    private const float GlobalEnd = 0.85f;

    private void ReportOwnedProgress(float local01)
    {
        float clamped = Mathf.Clamp01(local01);
        float global = Mathf.Lerp(GlobalStart, GlobalEnd, clamped);
        SceneLoader.SetProgressExternal(global);
    }

    private async void Start()
    {
        IsReady = false;

        // 0단계: 시작
        ReportOwnedProgress(0.0f);

        // 1) 세이브 데이터 / 캐릭터 테이블 준비까지 기다리기
        await UniTask.WaitUntil(() =>
            SaveLoadManager.Data != null &&
            DataTableManager.CharacterTable != null &&
            DataTableManager.StoryTable != null
        );
        ReportOwnedProgress(0.2f);

        // 2) 리스트 + 프리팹 생성
        BuildOwnedCharacterList();
        ReportOwnedProgress(0.5f);

        InstantiateCharacters();
        ReportOwnedProgress(0.8f);

        // 3) 레이아웃 강제 재계산
        await UniTask.Yield(); // 한 프레임 넘기고
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        // 4) 완료
        ReportOwnedProgress(1.0f);
        IsReady = true;
    }

    /// Save 데이터 기준으로 '보유 중인 캐릭터'만 SO 리스트로 만든다.
    /// 스토리 던전인 경우 need_char 필드에 따라 특정 캐릭터만 필터링
    private void BuildOwnedCharacterList()
    {
        _ownedCharacters.Clear();

        var saveData = SaveLoadManager.Data;
        if (saveData == null)
        {
            Debug.LogWarning("[OwnedCharacterSetup] SaveLoadManager.Data == null");
            return;
        }

        var charTable = DataTableManager.CharacterTable;
        if (charTable == null)
        {
            Debug.LogWarning("[OwnedCharacterSetup] CharacterTable == null");
            return;
        }

        // 스토리 던전 특정 캐릭터 필터링 체크
        string requiredCharacterName = GetRequiredCharacterForStoryStage(saveData.selectedStageID);

        // ownedIds 안에 현재 보유 캐릭터 ID가 들어있음
        foreach (int id in saveData.ownedIds)
        {
            var row = charTable.Get(id);   // CharacterCSVData
            if (row == null)
            {
                Debug.LogWarning($"[OwnedCharacterSetup] CharacterTable row null: id={id}");
                continue;
            }

            // 스토리 던전에서 특정 캐릭터만 필요한 경우 필터링
            if (!string.IsNullOrEmpty(requiredCharacterName) &&
                !row.char_name.Equals(requiredCharacterName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue; // 필요한 캐릭터가 아니면 스킵
            }

            // CSV에 있는 data_AssetName 기준으로 SO 로드 (너가 쓰는 패턴)
            var so = ResourceManager.Instance.Get<CharacterData>(row.data_AssetName);
            if (so == null)
            {
                Debug.LogWarning($"[OwnedCharacterSetup] CharacterData SO null: {row.data_AssetName}");
                continue;
            }

            _ownedCharacters.Add(so);
        }

        // 정렬 규칙 (등급, 레벨, 이름 내림차순)
        _ownedCharacters.Sort((a, b) =>
        {
            int cmp = b.char_rank.CompareTo(a.char_rank);
            if (cmp != 0) return cmp;

            cmp = b.char_lv.CompareTo(a.char_lv);
            if (cmp != 0) return cmp;

            return b.char_name.CompareTo(a.char_name);
        });

        if (!string.IsNullOrEmpty(requiredCharacterName))
        {
            Debug.Log($"[OwnedCharacterSetup] 스토리 던전 필터링: {requiredCharacterName} 캐릭터만 표시, 총 {_ownedCharacters.Count}개");
        }
        else
        {
            Debug.Log($"[OwnedCharacterSetup] OwnedCharacters = {_ownedCharacters.Count}");
        }
    }

    /// ScrollView Content 밑에 DragMe 프리팹들 생성
    private void InstantiateCharacters()
    {
        if (content == null || characterPrefab == null)
        {
            Debug.LogWarning("[OwnedCharacterSetup] content 또는 characterPrefab 미할당");
            return;
        }

        // 기존 자식들 정리 (리프레시 가능하게)
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        foreach (var characterData in _ownedCharacters)
        {
            var dragMeInstance = Instantiate(characterPrefab, content);
            dragMeInstance.name = characterData.char_name;

            // 1) DragMe에 데이터 꽂고
            dragMeInstance.characterData = characterData;

            // 2) CharacterSelectPanel도 바로 초기화
            var panel = dragMeInstance.GetComponent<CharacterSelectPanel>();
            if (panel != null)
                panel.Init(characterData);

            // 3) RectTransform 정리
            if (dragMeInstance.transform is RectTransform rect)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }
        }
    }

    /// 스토리 스테이지에서 필요한 특정 캐릭터 이름을 반환
    private string GetRequiredCharacterForStoryStage(int stageId)
    {
        // 스토리 던전 ID 범위 확인 (66001~66004 등)
        if (stageId < 66000 || stageId >= 67000)
        {
            return null; // 스토리 던전이 아님
        }

        var storyTable = DataTableManager.StoryTable;
        if (storyTable == null)
        {
            return null;
        }

        var storyStageData = storyTable.GetStoryStage(stageId);
        if (storyStageData == null)
        {
            return null;
        }

        // need_char 필드에서 필요한 캐릭터 이름 추출
        string needChar = storyStageData.need_char;
        if (string.IsNullOrEmpty(needChar) || needChar.Equals("none", System.StringComparison.OrdinalIgnoreCase))
        {
            return null; // 특정 캐릭터 제한 없음
        }

        return needChar;
    }

    /// 나중에 새 캐릭 획득 후 리스트 갱신하고 싶을 때 호출용
    public void Refresh()
    {
        BuildOwnedCharacterList();
        InstantiateCharacters();
    }
}

