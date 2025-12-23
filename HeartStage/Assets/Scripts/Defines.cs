using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Languages
{
    Korean,
}

public static class DataTableIds
{
    public static string Item => "ItemTable";
    public static string Character => "CharacterTable";
    public static string Monster => "MonsterTable";
    public static string StageWave => "StageWaveTable";
    public static string Skill => "SkillTable";
    public static string Effect => "EffectTable";
    public static string Stage => "StageTable";
    public static string Select => "SelectTable";
    public static string Synergy => "SynergyTable";
    public static string Reward => "RewardTable";
    public static string Gacha => "GachaTable";
    public static string GachaType => "GachaTypeTable";
    public static string Shop => "ShopTable";
    public static string RankUp => "RankUpTable";
    public static string LevelUp => "LevelUpTable";
    public static string Quest => "QuestTable";
    public static string QuestType => "QuestTypeTable";
    public static string QuestProgress => "QuestProgressTable";
    public static string Piece => "PieceTable";
    public static string Title => "TitleTable";
    public static string Slang => "SlangTable";
    public static string InfiniteStage => "InfiniteStageTable";
    public static string InfiniteMonster => "InfiniteMonsterTable";
    public static string Story => "StoryStageTable";
    public static string StoryScript => "StoryScriptTable";
    public static string Likeability => "LikeabilityTable";

    public static string TutorialScript => "TutorialCutSceneScript";
}

public class Tag
{
    public static readonly string Player = "Player";
    public static readonly string Monster = "Monster";
    public static readonly string Wall = "Wall";
    public static readonly string Tower = "Tower";
    public static readonly string LobbyHomeObject = "LobbyHomeObject";
}

public class Layer
{
    public static readonly string Character = "Character";
    public static readonly string Boss = "Boss";
}

public class AddressableLabel
{
    public static readonly string Stage = "StageAssets";
}

public enum WindowType
{
    None = -1,
    // 로비 윈도우
    LobbyHome = 0,
    StageSelect = 1,
    StageInfo = 2,
    Gacha = 3,
    GachaPercentage = 4,
    GachaResult = 5,
    Gacha5TryResult = 6,
    Quest = 7,
    GachaCancel = 8,
    MonitoringCharacterSelect = 9,
    MonitoringReward = 10,
    MailUI = 11,
    MailInfoUI = 12,
    SettingPanel = 13,
    Shopping = 14,
    CharacterDict = 15,
    SpecialDungeon = 16,
    StoryDungeon = 17,
    StoryDungeonInfo = 18,
    SpecialStage = 19,
    StoryStageRewardUI = 20,
    TutorialPanel = 21,
    TutorialNickNameWindow = 22,

    // 친구 관련 오버레이
    Friend = 30,            // 통합 친구 창 (목록/요청/추가 탭)
    FriendProfile = 33,     // 친구 프로필 팝업

    // 인게임 윈도우
    VictoryDefeat = 50, // 위에 추가해도 안바뀌게 큰 값으로 해두기
    CharacterInfo,
    LastStageNotice,
    LosePanelUI,
    VictoryPanelUI,
    BossAlert,
    StoryStageReward,
}

public enum SceneType
{
    None = -1,
    TitleScene = 0,
    LobbyScene = 1,
    StageScene = 2,
    TestStageScene = 3,
    TestStageWaveScene = 4,
    InfinityStage = 5,
    StoryScene = 6,
    TutorialCutScene = 7,
}

public class SoundName
{
    public static readonly string SFX_UI_Button_Click = "ui_click_01";
    public static readonly string SFX_UI_Exit_Button_Click = "ui_exit_click_01";
    public static readonly string SFX_UI_Skill_Select = "ui_skill_select_click_01";
    public static readonly string SFX_UI_Reward_Monitoring = "ui_reward_monitoring_01";
    public static readonly string SFX_UI_Enhance = "ui_enhance_01";
    public static readonly string SFX_UI_LevelUp = "ui_levelup_01";
    public static readonly string SFX_UI_Gacha_Result = "ui_gacha_result";
    public static readonly string SFX_UI_StageClear = "stage_clear_01";
    public static readonly string SFX_Boss_Appear = "boss_alert";
}

public class ItemID
{
    public static readonly int LightStick = 7101;
    public static readonly int HeartStick = 7102;
    public static readonly int Exp = 7103;
    public static readonly int DreamEnergy = 7104;
    public static readonly int TrainingPoint = 7105;
}

/// <summary>
/// 의상 타입 (ItemTable의 item_type과 동일)
/// </summary>
public enum CostumeType
{
    Top = 7,
    Pants = 8,
    Shoes = 9
}

/// <summary>
/// 의상 아이템 ID 베이스 및 유틸리티
/// </summary>
public static class CostumeItemID
{
    public static readonly int TopBase = 7401;
    public static readonly int PantsBase = 7501;
    public static readonly int ShoesBase = 7601;

    /// <summary>
    /// 아이템 ID로부터 스프라이트 폴더 번호 계산
    /// 예: 7401 → 1, 7402 → 2, 7405 → 5
    /// </summary>
    public static int GetSpriteId(int itemId)
    {
        if (itemId >= ShoesBase) return itemId - ShoesBase + 1;
        if (itemId >= PantsBase) return itemId - PantsBase + 1;
        if (itemId >= TopBase) return itemId - TopBase + 1;
        return 0;
    }

    /// <summary>
    /// 아이템 ID로부터 의상 타입 반환
    /// </summary>
    public static CostumeType GetCostumeType(int itemId)
    {
        if (itemId >= ShoesBase) return CostumeType.Shoes;
        if (itemId >= PantsBase) return CostumeType.Pants;
        return CostumeType.Top;
    }

    /// <summary>
    /// 의상 아이템인지 확인
    /// </summary>
    public static bool IsCostumeItem(int itemId)
    {
        return itemId >= TopBase && itemId < 7700;
    }
}

public class CurrencyIcon
{
    public static readonly string lightStickIcon = "Star-Stick-Yellow-128";
    public static readonly string heartStickIcon = "Star-Stick-Pink-128";
    public static readonly string dollarIcon = "DollarIcon";

    // 0 : 없음
    // 1 : 원화
    // 2 : 달러
    // 7101 : 라이트스틱
    // 7102 : 하트스틱

    public static void CurrencyIconChange(Image image, int id)
    {
        string iconAssetName = id switch
        {
            1 => dollarIcon,
            7101 => lightStickIcon,
            7102 => heartStickIcon,
            _ => null,
        };

        if (iconAssetName == null)
            return;

        image.sprite = ResourceManager.Instance.GetSprite(iconAssetName);
    }
}

public static class StatPower
{
    // 능력치 파워 = 능력치 * 기준값(BaseLine) * 영향도(Weight)
    private static float vocalBaseLine = 10f;
    private static float vocalWeight = 0.3f;

    private static float labBaseLine = 100f;
    private static float labWeight = 0.2f;

    private static float charismaBaseLine = 10f;
    private static float charismaWeight = 0.1f;

    private static float cutyBaseLine = 10f;
    private static float cutyWeight = 0.05f;

    private static float danceBaseLine = 1f;
    private static float danceWeight = 0.15f;

    private static float visualBaseLine = 10f;
    private static float visualWeight = 0.1f;

    private static float sexyBaseLine = 100f;
    private static float sexyWeight = 0.1f;

    //        Power Functions
    public static int GetVocalPower(float value)
    {
        return Mathf.CeilToInt(value * vocalBaseLine * vocalWeight);
    }

    public static int GetLabPower(float value)
    {
        return Mathf.CeilToInt(value * labBaseLine * labWeight);
    }

    public static int GetCharismaPower(float value)
    {
        return Mathf.CeilToInt(value * charismaBaseLine * charismaWeight);
    }

    public static int GetCutyPower(float value)
    {
        return Mathf.CeilToInt(value * cutyBaseLine * cutyWeight);
    }

    public static int GetDancePower(float value)
    {
        return Mathf.CeilToInt(value * danceBaseLine * danceWeight);
    }

    public static int GetVisualPower(float value)
    {
        return Mathf.CeilToInt(value * visualBaseLine * visualWeight);
    }

    public static int GetSexyPower(float value)
    {
        return Mathf.CeilToInt(value * sexyBaseLine * sexyWeight);
    }
}

public class CharacterAttributeIcon
{
    public static readonly string VocalIconAssetName = "VocalIcon";
    public static readonly string LabIconAssetName = "LabIcon";
    public static readonly string CharismaIconAssetName = "CharismaIcon";
    public static readonly string CutyIconAssetName = "CutyIcon";
    public static readonly string DanceIconAssetName = "DanceIcon";
    public static readonly string VisualIconAssetName = "VisualIcon";
    public static readonly string SexyIconAssetName = "SexyIcon";

    public enum CharacterAttribute
    {
        Vocal = 1,
        Lab = 2,
        Charisma = 3,
        Cuty = 4,
        Dance = 5,
        Visual = 6,
        Sexy = 7,
    }

    // 속성 타입 → 에셋 이름 매핑 딕셔너리
    private static readonly Dictionary<CharacterAttribute, string> iconNames =
        new Dictionary<CharacterAttribute, string>()
    {
        { CharacterAttribute.Vocal, VocalIconAssetName },
        { CharacterAttribute.Lab, LabIconAssetName },
        { CharacterAttribute.Charisma, CharismaIconAssetName },
        { CharacterAttribute.Cuty, CutyIconAssetName },
        { CharacterAttribute.Dance, DanceIconAssetName },
        { CharacterAttribute.Visual, VisualIconAssetName },
        { CharacterAttribute.Sexy, SexyIconAssetName },
    };

    // char_type에 따라 이미지 자동 변경
    public static void ChangeIcon(Image image, int char_type)
    {
        CharacterAttribute attr = (CharacterAttribute)char_type;

        string assetName = iconNames[attr];
        image.sprite = ResourceManager.Instance.GetSprite(assetName);
    }
}

public static class RankName
{
    public enum Rank
    {
        None = 0,
        ChickTrainee = 1,
        AceTrainee = 2,
        RookieIdol = 3,
        PopularIdol = 4
    }

    private static readonly string[] Names =
    {
        "없음",
        "병아리 연습생",
        "에이스 연습생",
        "신인 아이돌",
        "인기 아이돌"
    };

    public static string Get(int rank)
    {
        if (rank < 0 || rank >= Names.Length)
            return "알 수 없음";

        return Names[rank];
    }
}