using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryPrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stroyNameText;
    [SerializeField] private TextMeshProUGUI gradeText;

    [Header("캐릭터 설정")]
    [SerializeField] private string characterName; // Inspector에서 "하나" 또는 "세라" 설정

    [SerializeField] private Image storyImage;
    [SerializeField] private Image rewardImage1;
    [SerializeField] private Image rewardImage2;
    [SerializeField] private Image rewardImage3;
    [SerializeField] private Image rewardImage4;

    private void OnEnable()
    {
        UpdateGradeText();
    }

    /// 등급 텍스트 업데이트
    public void UpdateGradeText()
    {
        if (gradeText == null || string.IsNullOrEmpty(characterName)) return;

        var charData = GetOwnedCharacterData(characterName);
        if (charData != null)
        {
            gradeText.text = $"LV.{charData.char_lv} {RankName.Get(charData.char_rank)}";
        }
    }

    /// 캐릭터 이름으로 현재 보유 중인 캐릭터의 등급을 표시
    public void SetGradeByCharacterName(string characterName)
    {
        if (gradeText == null) return;

        var charData = GetOwnedCharacterData(characterName);
        if (charData != null)
        {
            gradeText.text = $"LV.{charData.char_lv} {RankName.Get(charData.char_rank)}";
        }
        else
        {
            gradeText.text = "미보유";
        }
    }

    /// 캐릭터 이름으로 보유 중인 캐릭터 데이터 반환 (가장 높은 등급)
    private CharacterCSVData GetOwnedCharacterData(string characterName)
    {
        if (SaveLoadManager.Data == null) return null;

        CharacterCSVData bestChar = null;

        foreach (var ownedId in SaveLoadManager.Data.ownedIds)
        {
            var charData = DataTableManager.CharacterTable.Get(ownedId);
            if (charData != null && charData.char_name == characterName)
            {
                if (bestChar == null || charData.char_rank > bestChar.char_rank)
                {
                    bestChar = charData;
                }
            }
        }

        return bestChar;
    }
}
