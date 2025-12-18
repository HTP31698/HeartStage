using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterHelper
{
    private static List<int> OwnCharacterIds => SaveLoadManager.Data.ownedIds;
    private static Dictionary<int, int> CharacterExpById => SaveLoadManager.Data.expById;
    private static Dictionary<string, bool> UnlockedByName => SaveLoadManager.Data.unlockedByName;
    private static List<string> OwnedProfileIconKeys => SaveLoadManager.Data.ownedProfileIconKeys;

    //캐릭터 획득 처리
    public static void AcquireCharacter(int baseId, CharacterTable charTable)
    {
        var data = SaveLoadManager.Data;
        if (data == null)
        {
            Debug.LogError("[CharacterHelper] SaveDataV1 가 null 입니다.");
            return;
        }

        if (charTable == null)
        {
            Debug.LogError("[CharacterHelper] charTable 이 null 입니다.");
            return;
        }

        var row = charTable.Get(baseId);
        if (row == null)
        {
            Debug.LogError($"[CharacterHelper] CharacterTable.Get({baseId}) 실패");
            return;
        }

        string name = row.char_name;
        string iconKey = row.icon_imageName;

        Debug.Log($"[CharacterHelper] AcquireCharacter baseId={baseId}, name={name}, iconKey={iconKey}");

        // 1) 도감/해금 true
        UnlockedByName[name] = true;

        // 2) 보유 id 등록 (중복 방지)
        if (!OwnCharacterIds.Contains(baseId))
            OwnCharacterIds.Add(baseId);

        // 3) exp 초기화
        if (!CharacterExpById.ContainsKey(baseId))
            CharacterExpById[baseId] = 0;

        // 4) 프로필 아이콘 획득 처리
        if (!string.IsNullOrEmpty(iconKey))
        {
            if (!OwnedProfileIconKeys.Contains(iconKey))
            {
                OwnedProfileIconKeys.Add(iconKey);
                Debug.Log($"[CharacterHelper] OwnedProfileIconKeys 에 '{iconKey}' 추가. Count={OwnedProfileIconKeys.Count}");
            }
            else
            {
                Debug.Log($"[CharacterHelper] 이미 OwnedProfileIconKeys 에 '{iconKey}' 존재");
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterHelper] icon_imageName 이 비어있음. name={name}");
        }

        SaveLoadManager.SaveToServer().Forget();
    }

    public static void ReplaceOwnedId(int currentId, int nextId, int remainExp)
    {
        //레벨 업 후 or 랭크 업 후 호출
        // id 교체 및 경험치 갱신
        int idx = OwnCharacterIds.IndexOf(currentId);
        if (idx < 0)
            return;

        OwnCharacterIds[idx] = nextId;

        CharacterExpById.Remove(currentId);
        CharacterExpById[nextId] = remainExp;
    }

    public static void CommitUpgradeResult(int startId, int finalId, int remainExp = 0)
    {
        //레벨 업/랭크 업 결과 확정 처리
        if (finalId != startId)
        {
            ReplaceOwnedId(startId, finalId, remainExp);
        }
        else
        {
            // 레벨업/랭크업 안 됐으면 exp만 업데이트
            CharacterExpById[startId] = remainExp;
        }

        SaveLoadManager.SaveToServer().Forget(); // 최종 1회 저장
    }

    /// <summary>
    /// 캐릭터 보유 여부 확인 (이름 기준)
    /// </summary>
    public static bool HasCharacter(int charId)
    {
        var row = DataTableManager.CharacterTable.Get(charId);
        if (row == null) return false;

        string name = row.char_name;
        return UnlockedByName.TryGetValue(name, out bool unlocked) && unlocked;
    }

    // 보유 캐릭터 등급 Get (이름 기준)
    public static int GetOwnedMaxRankByBaseId(int baseId)
    {
        var baseRow = DataTableManager.CharacterTable.Get(baseId);
        if (baseRow == null)
            return 0;

        string name = baseRow.char_name;

        int maxRank = 1;

        foreach (var ownedId in SaveLoadManager.Data.ownedIds)
        {
            var row = DataTableManager.CharacterTable.Get(ownedId);
            if (row == null)
                continue;

            if (row.char_name == name)
            {
                maxRank = Mathf.Max(maxRank, row.char_rank);
            }
        }

        return maxRank;
    }
    // 해당 캐릭터 호감도 수치 Get(이름 기준)
    public static int GetLikeability(string characterName)
    {
        var dict = SaveLoadManager.Data.likeabilityDict;
        if (!dict.ContainsKey(characterName))
            return 0;

        return dict[characterName];
    }
    // 해당 캐릭터 호감도 수치 세팅(이름 기준)
    public static void SetLikeability(string characterName, int amount)
    {
        var dict = SaveLoadManager.Data.likeabilityDict;
        dict[characterName] = amount;
        SaveLoadManager.SaveToServer().Forget();
    }
    // 호감도 보상 받았는지 상태 Return
    public static LikeabilityRewardState GetLikeabilityRewardState(string characterName)
    {
        var dict = SaveLoadManager.Data.likeabilityRewardStates;

        if (!dict.TryGetValue(characterName, out var state))
        {
            state = new LikeabilityRewardState();
            dict[characterName] = state;
        }

        return state;
    }
    // 호감도 보상 받기
    public static void ReceiveLikeabilityReward(string characterName, int rewardIndex, int itemId, int itemAmount)
    {
        var state = GetLikeabilityRewardState(characterName);

        switch (rewardIndex)
        {
            case 1:
                if (state.reward1Received) 
                    return;
                state.reward1Received = true;
                break;
            case 2:
                if (state.reward2Received) 
                    return;
                state.reward2Received = true;
                break;
            case 3:
                if (state.reward3Received)
                    return;
                state.reward3Received = true;
                break;
        }
        ItemInvenHelper.AddItem(itemId, itemAmount);
        SaveLoadManager.SaveToServer().Forget();
    }
}