using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class DailyShopSlot
{
    public int id;
    public bool purchased;

    public DailyShopSlot(int id, bool purchased)
    {
        this.id = id;
        this.purchased = purchased;
    }
}

public class DailyShop : MonoBehaviour
{
    public TextMeshProUGUI remainTimeText;
    public List<ShopItemSlot> dailyItemSlots;

    // 다음에 상점이 리셋될 남은 시간
    private DateTime nextResetTime;
    private const string ResetKey = "DailyShopNextResetTime";

    private void Start()
    {
        LoadOrInitResetTime();
        UpdateUI();        
    }

    private float timer = 0;
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 0.3f)
        {
            timer = 0;
            UpdateCountdown();
        }
    }

    private void LoadOrInitResetTime()
    {
        DateTime serverNow = FirebaseTime.GetServerTime();

        bool needReset = false;

        // 저장된 resetTime 불러오기
        if (PlayerPrefs.HasKey(ResetKey))
        {
            long binary = Convert.ToInt64(PlayerPrefs.GetString(ResetKey));
            nextResetTime = DateTime.FromBinary(binary);

            // 서버 시간이 리셋 시점을 넘었으면 리셋 필요
            if (serverNow >= nextResetTime)
                needReset = true;
        }
        else
        {
            // 저장된 리셋 시간이 없음 → 처음 실행
            needReset = true;
        }

        if (needReset)
        {
            ResetDailyShop();  // 슬롯 새로 생성
            // 다음 날 00:00:00으로 설정
            nextResetTime = serverNow.Date.AddDays(1);
            PlayerPrefs.SetString(ResetKey, nextResetTime.ToBinary().ToString());
        }
        else
        {
            var list = SaveLoadManager.Data.dailyShopSlotList;

            if (list.Count != 3)
            {
                ResetDailyShop();
                return;
            }

            // 리셋 안해도 됨 → 기존 슬롯 복원
            for (int i = 0; i < 3; i++)
            {
                dailyItemSlots[i].isDailyShopSlot = true;
                dailyItemSlots[i].Init(list[i].id, list[i].purchased);
            }
        }
    }

    // 서버 기준 남은 시간 계산
    private void UpdateCountdown()
    {
        DateTime serverNow = FirebaseTime.GetServerTime();
        TimeSpan remain = nextResetTime - serverNow; 

        if (remain.TotalSeconds <= 0)
        {
            ResetDailyShop();
            // 다음 날 00:00:00으로 설정
            nextResetTime = serverNow.Date.AddDays(1);
            PlayerPrefs.SetString(ResetKey, nextResetTime.ToBinary().ToString());
            remain = nextResetTime - serverNow;
        }

        remainTimeText.text = $"{remain.Hours:D2}:{remain.Minutes:D2}:{remain.Seconds:D2}";
    }

    // 판매품 리셋 (현재 조각만 판매 중)
    public void ResetDailyShop()
    {
        SaveLoadManager.Data.dailyShopSlotList.Clear();

        var randIds = DataTableManager.ShopTable.GetRandomThreePieceIds();
        for (int i = 0; i < 3; i++)
        {
            dailyItemSlots[i].isDailyShopSlot = true;
            dailyItemSlots[i].Init(randIds[i], false);
            SaveLoadManager.Data.dailyShopSlotList.Add(new DailyShopSlot(randIds[i],false));
        }
        // 구매 확인 창이 열려 있는 도중 바뀌는 경우 대처
        PurchaseConfirmPanel.Instance?.Close();
        // 저장
        SaveLoadManager.SaveToServer().Forget();
    }
    
    // 남은 시간 Text 업데이트
    private void UpdateUI()
    {
        DateTime serverNow = FirebaseTime.GetServerTime(); 
        TimeSpan remain = nextResetTime - serverNow; 

        remainTimeText.text = $"{remain.Hours:D2}:{remain.Minutes:D2}:{remain.Seconds:D2}";
    }
}