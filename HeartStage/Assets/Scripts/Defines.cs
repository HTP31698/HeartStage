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
}

public class Tag
{
    public static readonly string Player = "Player";
    public static readonly string Monster = "Monster";
    public static readonly string Wall = "Wall";
    public static readonly string Tower = "Tower";
    public static readonly string LobbyHomeObject = "LobbyHomeObject";
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

    // 인게임 윈도우
    VictoryDefeat = 50, // 위에 추가해도 안바뀌게 큰 값으로 해두기
    CharacterInfo,
    LastStageNotice,
    LosePanelUI,
    VictoryPanelUI,
    BossAlert,
}

public enum SceneType
{
    None = -1,
    TitleScene = 0,
    LobbyScene = 1,
    StageScene = 2,
    TestStageScene = 3,
    TestStageWaveScene = 4,
    InfiniteTestScene = 5,
}

public class SoundName
{
    public static readonly string SFX_UI_Button_Click = "ui_click_01";
    public static readonly string SFX_UI_Exit_Button_Click = "ui_exit_click_01";
    public static readonly string SFX_UI_Skill_Select = "ui_skill_select_click_01";
    public static readonly string SFX_UI_Reward_Monitoring = "ui_reward_monitoring_01";
    public static readonly string SFX_UI_Enhance = "ui_enhance_01";
    public static readonly string SFX_UI_LevelUp = "ui_levelup_01";
}

public class ItemID
{
    public static readonly int LightStick = 7101;
    public static readonly int HeartStick = 7102;
    public static readonly int Exp = 7103;
    public static readonly int DreamEnergy = 7104;
    public static readonly int TrainingPoint = 7105;
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

        var texture = ResourceManager.Instance.Get<Texture2D>(iconAssetName);

        if (texture == null)
            return;

        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}

public static class StatPower
{
    // 능력치 파워 = (능력치 / 기준값(BaseLine)) * 영향도(Weight)
    private static float vocalBaseLine = 1f;
    private static float vocalWeight = 0.3f;

    private static float labBaseLine = 0.01f;
    private static float labWeight = 0.2f;

    private static float charismaBaseLine = 0.05f;
    private static float charismaWeight = 0.1f;

    private static float cutyBaseLine = 0.05f;
    private static float cutyWeight = 0.05f;

    private static float danceBaseLine = 10f;
    private static float danceWeight = 0.15f;

    private static float visualBaseLine = 0.05f;
    private static float visualWeight = 0.1f;

    private static float sexyBaseLine = 0.01f;
    private static float sexyWeight = 0.1f;

    //        Power Functions
    public static int GetVocalPower(float value)
    {
        return Mathf.CeilToInt((value / vocalBaseLine) * vocalWeight);
    }

    public static int GetLabPower(float value)
    {
        return Mathf.CeilToInt((value / labBaseLine) * labWeight);
    }

    public static int GetCharismaPower(float value)
    {
        return Mathf.CeilToInt((value / charismaBaseLine) * charismaWeight);
    }

    public static int GetCutyPower(float value)
    {
        return Mathf.CeilToInt((value / cutyBaseLine) * cutyWeight);
    }

    public static int GetDancePower(float value)
    {
        return Mathf.CeilToInt((value / danceBaseLine) * danceWeight);
    }

    public static int GetVisualPower(float value)
    {
        return Mathf.CeilToInt((value / visualBaseLine) * visualWeight);
    }

    public static int GetSexyPower(float value)
    {
        return Mathf.CeilToInt((value / sexyBaseLine) * sexyWeight);
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

        var texture = ResourceManager.Instance.Get<Texture2D>(assetName);
        image.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}