using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
	public static LobbyManager Instance;

    public TextMeshProUGUI lightStickCountText;
    public TextMeshProUGUI heartStickCountText;
    public TextMeshProUGUI dreamEnergyCountText;
    public TextMeshProUGUI fanAmountText;

    private int lightStickCount = 0;
    public int LightStickCount
    {
        get { return lightStickCount; }
        set
        {
            lightStickCount = value;
            lightStickCountText.text = $"{lightStickCount}";
        }
    }

    private int heartStickCount = 0;
    public int HeartStickCount
    {
        get { return heartStickCount; }
        set
        {
            heartStickCount = value;
            heartStickCountText.text = $"{heartStickCount}";
        }
    }

    private int dreamEnergyCount = 0;
    public int DreamEnergyCount
    {
        get { return dreamEnergyCount; }
        set
        {
            dreamEnergyCount = value;
            dreamEnergyCountText.text = $"{value}/100";            
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        MoneyUISet();
    }

    // 외부(스테이지 버튼 등)에서 stageId 넣어 호출하는 표준 진입점
    public void GoStage(int stageId)
    {
        LoadSceneManager.Instance.GoStage(stageId, 1);
    }

    public void MoneyUISet()
    {
        var itemList = SaveLoadManager.Data.itemList;
        if (itemList.ContainsKey(ItemID.LightStick))
        {
            LightStickCount = itemList[ItemID.LightStick];
        }
        else
        {
            LightStickCount = 0;
        }

        if (itemList.ContainsKey(ItemID.HeartStick))
        {
            HeartStickCount = itemList[ItemID.HeartStick];
        }
        else
        {
            HeartStickCount = 0;
        }

        if (itemList.ContainsKey(ItemID.DreamEnergy))
        {
            DreamEnergyCount = itemList[ItemID.DreamEnergy];
        }
        else
        {
            DreamEnergyCount = 0;
        }

        fanAmountText.text = $"{SaveLoadManager.Data.fanAmount}";
    }

    // 테스트용 함수
    public void GetMoney() // 라이트 스틱, 하트 스틱, 트레이닝 포인트
    {
        ItemInvenHelper.AddItem(ItemID.LightStick, 5000);
        ItemInvenHelper.AddItem(ItemID.HeartStick, 1000);
        ItemInvenHelper.AddItem(ItemID.TrainingPoint, 100000);
        ItemInvenHelper.AddItem(ItemID.DreamEnergy, 1000);
        SaveLoadManager.SaveToServer().Forget();
    }
    public void SaveReset()
    {
        SaveLoadManager.Data = new SaveDataV1();
        MoneyUISet();

        var charTable = DataTableManager.CharacterTable;

        charTable.BuildDefaultSaveDictionaries(
            new[] {"하나", "세라", "리아" },                   // 스타터 이름만 여기
            out var unlockedByName,
            out var expById,
            out var ownedBaseIds
        );

        SaveLoadManager.Data.unlockedByName = unlockedByName;
        SaveLoadManager.Data.expById = expById;

        // 네가 current id 리스트/딕셔너리 어디에 들고있냐에 맞춰서
        foreach (var id in ownedBaseIds)
            SaveLoadManager.Data.ownedIds.Add(id); // List<int>면 이렇게

        // 처음에 드림에너지 100개 주기
        ItemInvenHelper.AddItem(ItemID.DreamEnergy, 100);
        SaveLoadManager.SaveToServer().Forget();

        //현재 출석은 로그인을 확인해야 해서 부트씬으로 가야해 적용 안됨
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
    }

    public void Logout()
    {
        AuthManager.Instance.SignOut();
    }
    //
}