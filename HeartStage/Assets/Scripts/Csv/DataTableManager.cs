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
        {
            var table = new ItemTable();
            await table.LoadAsync(DataTableIds.Item);
            tables.Add(DataTableIds.Item, table);
        }

        {
            var monsterTable = new MonsterTable();
            await monsterTable.LoadAsync(DataTableIds.Monster);
            tables.Add(DataTableIds.Monster, monsterTable);
        }

        {
            var stageWaveTable = new StageWaveTable();
            await stageWaveTable.LoadAsync(DataTableIds.StageWave);
            tables.Add(DataTableIds.StageWave, stageWaveTable);
        }

        {
            var table = new CharacterTable();
            var id = DataTableIds.Character;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new SkillTable();
            var id = DataTableIds.Skill;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        { 
            var table = new EffectTable();
            var id = DataTableIds.Effect;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new StageTable();
            var id = DataTableIds.Stage;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new SelectTable();
            var id = DataTableIds.Select;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        { 
            var table = new SynergyTable();
            var id = DataTableIds.Synergy;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new RewardTable();
            var id = DataTableIds.Reward;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new GachaTable();
            var id = DataTableIds.Gacha;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new GachaTypeTable();
            var id = DataTableIds.GachaType;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }
        
        {
        	var table = new ShopTable();
            var id = DataTableIds.Shop;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new RankUpTable();
            var id = DataTableIds.RankUp;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new LevelUpTable();
            var id = DataTableIds.LevelUp;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new QuestTable();
            var id = DataTableIds.Quest;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }


        {
            var table = new QuestTypeTable();
            var id = DataTableIds.QuestType;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new QuestProgressTable();
            var id = DataTableIds.QuestProgress;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new PieceTable();
            var id = DataTableIds.Piece;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }
        {
            var table = new TitleTable();
            var id = DataTableIds.Title;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }
        {
            var table = new SlangTable();
            var id = DataTableIds.Slang;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

        {
            var table = new InfiniteStageTable();
            var id = DataTableIds.InfiniteStage;
            await table.LoadAsync(id);
            tables.Add(id, table);
        }

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
