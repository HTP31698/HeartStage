using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Gacha5TryResultUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject gachaResultItemPrefab;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retryButton;

    private List<GameObject> gachaItemList = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public override void Open()
    {
        base.Open();

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Gacha_Result);

        if (GachaUI.gachaFiveResultReceiver != null && GachaUI.gachaFiveResultReceiver.Count > 0)
        {
            DisplayResults(GachaUI.gachaFiveResultReceiver);
            GachaUI.gachaFiveResultReceiver = null; // 결과 사용 후 초기화
        }
    }
    public override void Close()
    {
        base.Close();
        ClearResults();
    }

    // 5개 가챠 결과 표시
    private void DisplayResults(List<GachaResult> results)
    {
        ClearResults();

        foreach (var gachaItems in results)
        {
            if(gachaResultItemPrefab != null && contentParent != null)
            {
                var item = Instantiate(gachaResultItemPrefab, contentParent);
                var prefabUI = item.GetComponent<Gacha5TryResultPrefabUI>();

                if(prefabUI != null)
                {
                    prefabUI.Init(gachaItems);
                }
                gachaItemList.Add(item);
            }
        }
    }
    private void ClearResults()
    {
        foreach (var item in gachaItemList)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        gachaItemList.Clear();
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    private void OnRetryButtonClicked()
    {
        // 새로운 5개 뽑기 실행
        var gachaResults = GachaManager.Instance.DrawGachaFiveTimes(2); // 기본 캐릭터 가챠

        if (gachaResults != null && gachaResults.Count > 0)
        {
            DisplayResults(gachaResults);
        }
        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.GachaCancel);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }
}
