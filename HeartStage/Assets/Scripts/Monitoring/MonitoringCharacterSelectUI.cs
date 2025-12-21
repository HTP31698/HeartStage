using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonitoringCharacterSelectUI : GenericWindow
{
    [Header("Character List")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterPrefab;

    [Header("Selected Character Slots")]
    [SerializeField] private MonitoringCharacterSlot[] selectedSlots = new MonitoringCharacterSlot[3];

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button startButton;

    private int currentDipatchMemberCount = 3;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);

        // 슬롯 초기화
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null)
            {
                selectedSlots[i].Init(i, this);
            }
        }
    }

    public override void Open()
    {
        base.Open();
        UpdateDispatchMemberCount();
        Display(); // 창이 열릴 때 캐릭터들을 표시
        UpdateStartButton();
    }

    public override void Close()
    {
        base.Close();
        
    }

    private void UpdateDispatchMemberCount()
    {
        var gameData = SaveLoadManager.Data;

        if(gameData != null && gameData.selectedStageID != -1)
        {
            var stageData = DataTableManager.StageTable.GetStageData(gameData.selectedStageID);
            if (stageData != null)
            {
                currentDipatchMemberCount = stageData.dispatch_member;
            }
        }

        UpdateSlotState();
        UpdateStartButton(); // 슬롯 상태 변경 후 시작 버튼 업테이트
    }

    private void UpdateSlotState()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null)
            {
                bool isSlotEnabled = i < currentDipatchMemberCount;
                selectedSlots[i].SetSlotEnabled(isSlotEnabled);

                // 비활성화된 슬롯의 캐릭터는 제거
                if (!isSlotEnabled && !selectedSlots[i].IsEmpty())
                {
                    var characterData = selectedSlots[i].GetCharacterData();
                    selectedSlots[i].ClearSlot();
                    SetCharacterLocked(characterData, false);
                }
            }
        }
    }

    private void Display()
    {
        var allCharacters = GetOwnedCharacters();

        // 기존 프리팹들 제거
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        // 소유한 캐릭터들로 프리팹 생성
        foreach (var characterData in allCharacters)
        {
            var prefabInstance = Instantiate(characterPrefab, content);
            prefabInstance.name = characterData.char_name;

            // MonitoringCharacterPrefab 컴포넌트 초기화 (드래그 기능 포함)
            var monitoringPrefab = prefabInstance.GetComponent<MonitoringCharacterPrefab>();
            if (monitoringPrefab != null)
            {
                monitoringPrefab.Init(characterData);
            }

            // RectTransform 정리
            if (prefabInstance.transform is RectTransform rect)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = Vector3.zero;
            }
        }
    }

    // 슬롯에 캐릭터를 배치하는 메서드
    public bool TryPlaceCharacter(CharacterData characterData, int targetSlotIndex = -1)
    {
        // 파견 가능 여부 확인
        if (!CanCharacterDispatch(characterData))
        {
            Debug.Log($"{characterData.char_name}은(는) 파견 횟수가 부족합니다.");
            return false;
        }

        // 이미 배치된 캐릭터인지 확인
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null && selectedSlots[i].GetCharacterData() == characterData)
            {
                return false; // 이미 배치됨
            }
        }

        // 특정 슬롯이 지정된 경우
        if (targetSlotIndex >= 0 && targetSlotIndex < selectedSlots.Length)
        {
            if (selectedSlots[targetSlotIndex] != null)
            {
                if (targetSlotIndex >= currentDipatchMemberCount)
                {
                    return false; // 비활성화된 슬롯에는 배치 불가
                }

                bool success = selectedSlots[targetSlotIndex].TrySetCharacter(characterData);
                if (success)
                {
                    // 해당 캐릭터 프리팹을 잠금 상태로 변경
                    SetCharacterLocked(characterData, true);
                    UpdateStartButton();
                }
                return success;
            }
        }

        // 빈 슬롯 찾아서 배치
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null && selectedSlots[i].IsEmpty())
            {
                bool success = selectedSlots[i].TrySetCharacter(characterData);
                if (success)
                {
                    // 해당 캐릭터 프리팹을 잠금 상태로 변경
                    SetCharacterLocked(characterData, true);
                    UpdateStartButton();
                }
                return success;
            }
        }

        return false; // 모든 슬롯이 꽉 참

    }

    // 캐릭터의 파견 가능 여부 확인
    private bool CanCharacterDispatch(CharacterData characterData)
    {
        for (int i = 0; i < content.childCount; i++)
        {
            var prefab = content.GetChild(i).GetComponent<MonitoringCharacterPrefab>();
            if (prefab != null && prefab.GetCharacterData() == characterData)
            {
                return prefab.CanDispatch();
            }
        }
        return false;
    }

    // 슬롯에서 캐릭터를 제거하는 메서드
    public void RemoveCharacterFromSlot(CharacterData characterData)
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null && selectedSlots[i].GetCharacterData() == characterData)
            {
                selectedSlots[i].ClearSlot();
                // 해당 캐릭터 프리팹의 잠금 해제
                SetCharacterLocked(characterData, false);
                UpdateStartButton(); 
                break;
            }
        }
    }

    // 캐릭터 프리팹의 잠금 상태 변경
    public void SetCharacterLocked(CharacterData characterData, bool locked)
    {
        for (int i = 0; i < content.childCount; i++)
        {
            var prefab = content.GetChild(i).GetComponent<MonitoringCharacterPrefab>();
            if (prefab != null && prefab.GetCharacterData() == characterData)
            {
                prefab.SetLocked(locked);

                if (!locked)
                {
                    prefab.SetHighlighted(false);
                }

                break;
            }
        }
    }

    // 선택된 캐릭터들 가져오기
    public List<CharacterData> GetSelectedCharacters()
    {
        List<CharacterData> selected = new List<CharacterData>();

        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null && !selectedSlots[i].IsEmpty())
            {
                selected.Add(selectedSlots[i].GetCharacterData());
            }
        }

        return selected;
    }

    private List<CharacterData> GetOwnedCharacters()
    {
        List<CharacterData> ownedCharacters = new List<CharacterData>();

        var saveData = SaveLoadManager.Data;
        if (saveData == null)
        {
            return ownedCharacters;
        }

        var charTable = DataTableManager.CharacterTable;
        if (charTable == null)
        {
            return ownedCharacters;
        }

        // SaveLoadManager.Data.ownedIds에서 소유한 캐릭터 ID들을 가져와서 CharacterData로 변환
        foreach (int id in saveData.ownedIds)
        {
            var csvData = charTable.Get(id);
            if (csvData == null)
            {
                continue;
            }

            // CSV의 data_AssetName을 사용해 ScriptableObject 로드
            var characterData = ResourceManager.Instance.Get<CharacterData>(csvData.data_AssetName);
            if (characterData == null)
            {
                continue;
            }

            ownedCharacters.Add(characterData);
        }

        // 캐릭터 정렬 (등급 내림차순 -> 레벨 내림차순 -> 이름 내림차순)
        ownedCharacters.Sort((a, b) =>
        {
            int cmp = b.char_rank.CompareTo(a.char_rank);
            if (cmp != 0) return cmp;

            cmp = b.char_lv.CompareTo(a.char_lv);
            if (cmp != 0) return cmp;

            return b.char_name.CompareTo(a.char_name);
        });

        return ownedCharacters;
    }

    private void UpdateStartButton() // 시작 버튼 활성화 상태 업데이트
    {
        var selectedCharacters = GetSelectedCharacters();
        bool canStart = selectedCharacters.Count >= currentDipatchMemberCount;

        startButton.interactable = canStart;
    }
    private void OnStartButtonClicked()
    {      
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        var selectedCharacters = GetSelectedCharacters();

        if (selectedCharacters.Count < currentDipatchMemberCount)
        {
            return;
        }

        // 선택된 캐릭터들의 파견 횟수 차감
        bool anyDispatchConsumed = false;
        foreach (var character in selectedCharacters)
        {
            // 해당 캐릭터의 프리팹을 찾아서 파견 횟수 차감
            for (int i = 0; i < content.childCount; i++)
            {
                var prefab = content.GetChild(i).GetComponent<MonitoringCharacterPrefab>();
                if (prefab != null && prefab.GetCharacterData() == character)
                {
                    if (prefab.ConsumeDispatch())
                    {
                        anyDispatchConsumed = true;
                    }
                    break;
                }
            }
        }

        if (!anyDispatchConsumed)
        {
            return;
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Reward_Monitoring);

        GiveMonitoringReward();
        ClearAllSlots();

        WindowManager.Instance.OpenOverlay(WindowType.MonitoringReward);
    }


    private void ClearAllSlots()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i] != null)
            {
                selectedSlots[i].ClearSlot();
            }
        }

        // 캐릭터 목록 UI도 새로고침 (파견 횟수 변경 반영)
        UpdateStartButton();
        Display();
    }

    private void GiveMonitoringReward()
    {
        // 현재 선택된 스테이지 정보 가져오기
        var gameData = SaveLoadManager.Data;
        int currentStageID = gameData.selectedStageID;

        if (currentStageID == -1)
        {
            Debug.Log("선택된 스테이지가 없습니다.");
            return;
        }

        var stageData = DataTableManager.StageTable?.GetStageData(currentStageID);
        if (stageData == null)
        {
            return;
        }

        int rewardId = stageData.dispatch_reward;
        if (rewardId <= 0)
        {
            return;
        }

        // RewardTable에서 실제 보상 데이터 가져오기
        var rewardData = DataTableManager.RewardTable?.Get(rewardId);
        if (rewardData == null)
        {
            return;
        }

        // 실제 보상 아이템들 지급
        GiveRewardItems(rewardData);

        Debug.Log($"모니터링 보상 지급: {rewardData.reward_name}");

        // 서버에 저장
        SaveLoadManager.SaveToServer().Forget();
    }


    // 보상 아이템 지급
    private void GiveRewardItems(RewardData rewardData)
    {
        var saveItemList = SaveLoadManager.Data.itemList;

        // normal_clear1 (첫 번째 일반 보상)
        if (rewardData.normal_clear1 > 0 && rewardData.normal_clear1_a > 0)
        {
            AddItemToInventory(saveItemList, rewardData.normal_clear1, rewardData.normal_clear1_a);
            Debug.Log($"보상 1: 아이템 ID {rewardData.normal_clear1} x {rewardData.normal_clear1_a}개");
        }

        // normal_clear2 (두 번째 일반 보상)
        if (rewardData.normal_clear2 > 0 && rewardData.normal_clear2_a > 0)
        {
            AddItemToInventory(saveItemList, rewardData.normal_clear2, rewardData.normal_clear2_a);
            Debug.Log($"보상 2: 아이템 ID {rewardData.normal_clear2} x {rewardData.normal_clear2_a}개");
        }

        // normal_clear3 (세 번째 일반 보상)
        if (rewardData.normal_clear3 > 0 && rewardData.normal_clear3_a > 0)
        {
            AddItemToInventory(saveItemList, rewardData.normal_clear3, rewardData.normal_clear3_a);
            Debug.Log($"보상 3: 아이템 ID {rewardData.normal_clear3} x {rewardData.normal_clear3_a}개");
        }
    }

    // 인벤토리에 아이템 추가
    private void AddItemToInventory(Dictionary<int, int> itemList, int itemId, int amount)
    {
        if (itemList.ContainsKey(itemId))
        {
            itemList[itemId] += amount;
        }
        else
        {
            itemList.Add(itemId, amount);
        }
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        ClearAllSlots();
        Close();
    }
}