using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GachaPercentageUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject percentageInfoPrefab;

    [SerializeField] private Button closeButton;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();   

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public override void Open()
    {
        base.Open();
        DisplayGachaPercentage();
    }
    public override void Close()
    {
        base.Close();
        ClearSpawnedItem();
    }
    
    private void DisplayGachaPercentage()
    {
        ClearSpawnedItem();

        var allGachaTypes = DataTableManager.GachaTypeTable.GetAllData();

        foreach (var gachaType in allGachaTypes)
        {
            if(gachaType.Gacha_type_ID == 1) // 튜토리얼 제외
            {
                continue;
            }

            var gachaItems = GachaTable.GetGachaByType(gachaType.Gacha_type_ID);

            if(gachaItems == null || gachaItems.Count == 0)
            {
                continue;
            }

            // 가챠 타입별로 아이템들 표시
            foreach (var gachaData in gachaItems)
            {
                CreateGachaPercentageItem(gachaData);
            }
        }
    }

    private void CreateGachaPercentageItem(GachaData gachaData)
    {
        if (percentageInfoPrefab == null || content == null)
        {
            return;
        }

        // 프리팹 인스턴스 생성
        var itemObject = Instantiate(percentageInfoPrefab, content);
        spawnedItems.Add(itemObject);

        // GachaPercentageItemPrefab 컴포넌트 가져와서 초기화
        var itemPrefab = itemObject.GetComponent<GachaPercentageItemPrefab>();
        if (itemPrefab != null)
        {
            itemPrefab.Init(gachaData, 1); // 수량은 기본 1개
        }
        else
        {
            Debug.LogError("GachaPercentageItemPrefab 컴포넌트를 찾을 수 없습니다.");
        }

        // 위치
        if (itemObject.transform is RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition3D = Vector3.zero;
        }
    }

    private void ClearSpawnedItem()
    {
        foreach(var item in spawnedItems)
        {
            if(item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }
}
