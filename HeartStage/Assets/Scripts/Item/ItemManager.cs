using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    private Dictionary<int, ItemData> itemDB = new Dictionary<int, ItemData>();
    public static readonly string ItemPoolId = "ItemBase";

    public GameObject DropItem;
    public RectTransform itemBagTr;
    public RectTransform ExpTargetTr;

    public TextMeshProUGUI lightStickCountText;
    [HideInInspector]
    public Dictionary<int, int> acquireItemList = new Dictionary<int, int>();
    [HideInInspector]
    public int lightStickCount = 0;

    private Vector3 inventoryItemTarget;
    private Vector3 expItemTarget;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        inventoryItemTarget = GetUIToWorldPosition(itemBagTr);
        expItemTarget = GetUIToWorldPosition(ExpTargetTr);
        PoolManager.Instance.CreatePool(ItemPoolId, DropItem, 30, 120);
        SetItemCountUI();
    }

    public void SpawnItem(int itemId, int amount, Vector3 spawnPos)
    {
        var go = PoolManager.Instance.Get(ItemPoolId);
        var dropItem = go.GetComponent<DropItem>();

        // 스폰 위치 랜덤값 +
        Vector2 offset = Random.insideUnitCircle * 0.4f;
        spawnPos += (Vector3)offset;

        if (itemId == ItemID.Exp) // 경험치
        {
            dropItem.Setup(itemId, amount, spawnPos, expItemTarget);
        }
        else
        {
            dropItem.Setup(itemId, amount, spawnPos, inventoryItemTarget);
        }
    }

    // 아이템 사용하기
    public void UseItem(int itemId, int amount)
    {
        if(itemId == ItemID.Exp) // 경험치
        {
            StageManager.Instance.ExpGet(amount);
        }
        else
        {
            AcquireItem(itemId, amount);
        }
    }

    // 아이템 획득
    public void AcquireItem(int itemId, int amount)
    {
        if (acquireItemList.ContainsKey(itemId))
        {
            acquireItemList[itemId] += amount;
        }
        else
        {
            acquireItemList.Add(itemId, amount);
        }

        if(ItemID.LightStick == itemId)
        {
            lightStickCount += amount;
            SetItemCountUI();
        }
    }

    // 획득 아이템 개수 UI Set
    private void SetItemCountUI()
    {
        lightStickCountText.text = $"X{lightStickCount}";
    }

    // UI 위치의 월드좌표 얻기
    private Vector3 GetUIToWorldPosition(RectTransform ui)
    {
        // Overlay canvas라면 ui.position이 이미 스크린 좌표임
        Vector3 screenPos = ui.position;

        // 아이템과 카메라 사이 거리(양수!)
        float distance = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        screenPos.z = distance;

        // 스크린 → 월드 변환
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
}