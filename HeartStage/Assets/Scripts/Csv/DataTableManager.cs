using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public static class DataTableManager
{
    private static readonly Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();
    private static UniTask _initialization;

    public static UniTask Initialization => _initialization;

    static DataTableManager()
    {
        _initialization = InitAsync();
    }

    public static async UniTask InitAsync()
    {
        // 모든 테이블 인스턴스 생성
        var itemTable = new ItemTable();
        var monsterTable = new MonsterTable();
        var stageWaveTable = new StageWaveTable();
        var characterTable = new CharacterTable();
        var skillTable = new SkillTable();
        var effectTable = new EffectTable();
        var stageTable = new StageTable();
        var selectTable = new SelectTable();
        var synergyTable = new SynergyTable();
        var rewardTable = new RewardTable();
        var gachaTable = new GachaTable();
        var gachaTypeTable = new GachaTypeTable();
        var shopTable = new ShopTable();
        var rankUpTable = new RankUpTable();
        var levelUpTable = new LevelUpTable();
        var questTable = new QuestTable();
        var questTypeTable = new QuestTypeTable();
        var questProgressTable = new QuestProgressTable();
        var pieceTable = new PieceTable();
        var titleTable = new TitleTable();
        var slangTable = new SlangTable();
        var infiniteStageTable = new InfiniteStageTable();

        // 모든 테이블 병렬 로드
        await UniTask.WhenAll(
            itemTable.LoadAsync(DataTableIds.Item),
            monsterTable.LoadAsync(DataTableIds.Monster),
            stageWaveTable.LoadAsync(DataTableIds.StageWave),
            characterTable.LoadAsync(DataTableIds.Character),
            skillTable.LoadAsync(DataTableIds.Skill),
            effectTable.LoadAsync(DataTableIds.Effect),
            stageTable.LoadAsync(DataTableIds.Stage),
            selectTable.LoadAsync(DataTableIds.Select),
            synergyTable.LoadAsync(DataTableIds.Synergy),
            rewardTable.LoadAsync(DataTableIds.Reward),
            gachaTable.LoadAsync(DataTableIds.Gacha),
            gachaTypeTable.LoadAsync(DataTableIds.GachaType),
            shopTable.LoadAsync(DataTableIds.Shop),
            rankUpTable.LoadAsync(DataTableIds.RankUp),
            levelUpTable.LoadAsync(DataTableIds.LevelUp),
            questTable.LoadAsync(DataTableIds.Quest),
            questTypeTable.LoadAsync(DataTableIds.QuestType),
            questProgressTable.LoadAsync(DataTableIds.QuestProgress),
            pieceTable.LoadAsync(DataTableIds.Piece),
            titleTable.LoadAsync(DataTableIds.Title),
            slangTable.LoadAsync(DataTableIds.Slang),
            infiniteStageTable.LoadAsync(DataTableIds.InfiniteStage)
        );

        // Dictionary에 등록
        tables.Add(DataTableIds.Item, itemTable);
        tables.Add(DataTableIds.Monster, monsterTable);
        tables.Add(DataTableIds.StageWave, stageWaveTable);
        tables.Add(DataTableIds.Character, characterTable);
        tables.Add(DataTableIds.Skill, skillTable);
        tables.Add(DataTableIds.Effect, effectTable);
        tables.Add(DataTableIds.Stage, stageTable);
        tables.Add(DataTableIds.Select, selectTable);
        tables.Add(DataTableIds.Synergy, synergyTable);
        tables.Add(DataTableIds.Reward, rewardTable);
        tables.Add(DataTableIds.Gacha, gachaTable);
        tables.Add(DataTableIds.GachaType, gachaTypeTable);
        tables.Add(DataTableIds.Shop, shopTable);
        tables.Add(DataTableIds.RankUp, rankUpTable);
        tables.Add(DataTableIds.LevelUp, levelUpTable);
        tables.Add(DataTableIds.Quest, questTable);
        tables.Add(DataTableIds.QuestType, questTypeTable);
        tables.Add(DataTableIds.QuestProgress, questProgressTable);
        tables.Add(DataTableIds.Piece, pieceTable);
        tables.Add(DataTableIds.Title, titleTable);
        tables.Add(DataTableIds.Slang, slangTable);
        tables.Add(DataTableIds.InfiniteStage, infiniteStageTable);
    }

    public static ItemTable ItemTable
    {
        get
        {
            return Get<ItemTable>(DataTableIds.Item);
        }
    }
    
    public static MonsterTable MonsterTable
    {
        get
        {
            return Get<MonsterTable>(DataTableIds.Monster);
        }
    }

    public static StageWaveTable StageWaveTable
    {
        get
        {
            return Get<StageWaveTable>(DataTableIds.StageWave);
        }
    }

    public static CharacterTable CharacterTable
    {
        get
        {
            return Get<CharacterTable>(DataTableIds.Character);
        }
    }
    
    public static SkillTable SkillTable
    {
        get
        {
            return Get<SkillTable>(DataTableIds.Skill);
        }
    }
    public static EffectTable EffectTable
    {
        get
        {
            return Get<EffectTable>(DataTableIds.Effect);
        }
    }
    public static StageTable StageTable
    {
        get
        {
            return Get<StageTable>(DataTableIds.Stage);
        }
    }

    public static SelectTable SelectTable
    {
        get
        {
            return Get<SelectTable>(DataTableIds.Select);
        }
    }

    public static SynergyTable SynergyTable
    {
        get
        {
            return Get<SynergyTable>(DataTableIds.Synergy);
        }
    }

    public static RewardTable RewardTable
    {
        get
        {
            return Get<RewardTable>(DataTableIds.Reward);
        }
    }

    public static GachaTable GachaTable
    {
        get
        {
            return Get<GachaTable>(DataTableIds.Gacha);
        }
    }

    public static GachaTypeTable GachaTypeTable
    {
        get
        {
            return Get<GachaTypeTable>(DataTableIds.GachaType);
        }
    }
    
    public static ShopTable ShopTable
    {
        get
        {
            return Get<ShopTable>(DataTableIds.Shop);
        }
    }

    public static RankUpTable RankUpTable
    {
        get
        {
            return Get<RankUpTable>(DataTableIds.RankUp);
        }
    }

    public static LevelUpTable LevelUpTable
    {
        get
        {
            return Get<LevelUpTable>(DataTableIds.LevelUp);
        }
    }

    public static QuestTable QuestTable
    {
        get
        {
            return Get<QuestTable>(DataTableIds.Quest);
        }
    }
    public static QuestTypeTable QuestTypeTable
    {
        get
        {
            return Get<QuestTypeTable>(DataTableIds.QuestType);
        }
    }
    public static QuestProgressTable QuestProgressTable
    {
        get
        {
            return Get<QuestProgressTable>(DataTableIds.QuestProgress);
        }
    }

    public static PieceTable PieceTable
    {
        get
        {
            return Get<PieceTable>(DataTableIds.Piece);
        }
    }

    public static TitleTable TitleTable
    {
        get
        {
            return Get<TitleTable>(DataTableIds.Title);
        }
    }

    public static SlangTable SlangTable
    {
        get
        {
            return Get<SlangTable>(DataTableIds.Slang);
        }
    }

    public static InfiniteStageTable InfiniteStageTable
    {
        get
        {
            return Get<InfiniteStageTable>(DataTableIds.InfiniteStage);
        }
    }

    public static T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.LogError("테이블 없음");
            return null;
        }
        return tables[id] as T;
    }
}
