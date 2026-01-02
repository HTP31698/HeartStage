using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI itemCountText;  // 아이템 개수 (별도 표시)

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button reTryButton;

    private GachaResult gachaResult;
    private Sprite currentSprite; // 현재 스프라이트 참조 저장

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        reTryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public override void Open()
    {
        base.Open();

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Gacha_Result);

        if (GachaUI.gachaResultReciever.HasValue)
        {
            SetGachaResult(GachaUI.gachaResultReciever.Value);
            GachaUI.gachaResultReciever = null; // 결과 사용 후 초기화
        }

        DisPlayResult();
    }

    public override void Close()
    {
        base.Close();
        //ClearCurrentSprite(); // 창 닫을 때 스프라이트 정리
    }

    public void SetGachaResult(GachaResult result)
    {
        gachaResult = result;
    }

    private void DisPlayResult()
    {
        var characterData = gachaResult.characterData;
        var gachaData = gachaResult.gachaData;

        if(characterData != null)
        {
            if (gachaData.Gacha_have > 0 && gachaResult.isDuplicate)
            {
                var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_have);
                if (itemData != null)
                {
                    SetImage(itemData.prefab);
                    SetCharacterNameText(itemData.item_name);
                    SetItemCountText(gachaData.Gacha_have_amount);
                    return;
                }
            }

            SetImage(characterData.card_imageName);
            SetCharacterNameText(characterData.char_name);
            SetItemCountText(0);  // 캐릭터는 개수 표시 안함
        }

        else
        {
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetImage(itemData.prefab);
                SetCharacterNameText(itemData.item_name);
                SetItemCountText(gachaData.Gacha_item_amount);
            }
            else
            {
                SetCharacterNameText($"아이템 ID: {gachaData.Gacha_item}");
                SetItemCountText(0);
            }
        }
    }

    private void SetImage(string imageName)
    {
        if (characterImage == null || string.IsNullOrEmpty(imageName))
        {
            return;
        }

        // 기존 스프라이트 정리
        //ClearCurrentSprite();

        currentSprite = ResourceManager.Instance.GetSprite(imageName);
        characterImage.sprite = currentSprite;
    }

    //private void ClearCurrentSprite()
    //{
    //    if (currentSprite != null)
    //    {
    //        DestroyImmediate(currentSprite);
    //        currentSprite = null;
    //    }
    //}

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    private void OnRetryButtonClicked()
    {
        var gachaResult = GachaManager.Instance.DrawGacha(2); // 2는 캐릭터 가챠 타입 

        if (gachaResult.HasValue)
        {
            SetGachaResult(gachaResult.Value);
            DisPlayResult();
        }
        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.GachaCancel);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    private void SetCharacterNameText(string name)
    {
        if (characterNameText != null)
        {
            characterNameText.text = name;
        }
    }

    private void SetItemCountText(int count)
    {
        if (itemCountText != null)
        {
            if (count > 0)
            {
                itemCountText.gameObject.SetActive(true);
                itemCountText.text = $"x{count}";
            }
            else
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        // 컴포넌트 파괴시 스프라이트 정리
        //ClearCurrentSprite();
    }
}