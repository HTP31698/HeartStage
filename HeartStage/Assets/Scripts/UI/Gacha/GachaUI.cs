using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private int currentGachaTypeId = 2; // 기본값 2: 캐릭터 가챠

    [Header("Button")]
    [SerializeField] private Button percentageInfoButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button gachaFiveButton;

    // 간단한 정적 변수로 결과 전달
    public static GachaResult? gachaResultReciever;
    public static List<GachaResult> gachaFiveResultReceiver;

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        percentageInfoButton.onClick.AddListener(OnGachaPercentageInfoButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonOnClicked);
        gachaFiveButton.onClick.AddListener(OnGachaFiveButtonClicked);
    }

    private void OnGachaPercentageInfoButtonClicked()
    {
        WindowManager.Instance.OpenOverlay(WindowType.GachaPercentage);
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    // 1회 가챠 뽑기 버튼 클릭
    private void OnGachaButtonOnClicked()
    {
        var gachaResult = GachaManager.Instance.DrawGacha(currentGachaTypeId);

        if (gachaResult.HasValue)
        {
            // 정적 변수에 결과 저장
            gachaResultReciever = gachaResult.Value;

            // 결과창 열기
            WindowManager.Instance.OpenOverlay(WindowType.GachaResult);
        }
        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.GachaCancel);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    // 5회 가챠 뽑기 버튼 클릭
    private void OnGachaFiveButtonClicked()
    {
        var gachaResults = GachaManager.Instance.DrawGachaFiveTimes(currentGachaTypeId);

        if (gachaResults != null && gachaResults.Count > 0)
        {
            gachaFiveResultReceiver = gachaResults;
            WindowManager.Instance.OpenOverlay(WindowType.Gacha5TryResult);
        }

        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.GachaCancel);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }
}