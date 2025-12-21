using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button reTryButton;

    private GachaResult gachaResult;
    private Sprite currentSprite; // 현재 스프라이트 참조 저장

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        reTryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public override void Open()
    {
        base.Open();

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
        ClearCurrentSprite(); // 창 닫을 때 스프라이트 정리
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
                    SetCharacterNameText($"{itemData.item_name} x{gachaData.Gacha_have_amount}");
                    return;
                }
            }

            SetImage(characterData.card_imageName);
            SetCharacterNameText(characterData.char_name);
        }

        else
        {
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetImage(itemData.prefab);
                SetCharacterNameText($"{itemData.item_name}x{gachaData.Gacha_item_amount}");
            }
            else
            {
                SetCharacterNameText($"아이템 ID: {gachaData.Gacha_item}");
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
        ClearCurrentSprite();

        var texture = ResourceManager.Instance.Get<Texture2D>(imageName);
        if (texture != null)
        {
            currentSprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            characterImage.sprite = currentSprite;
        }
        else
        {
            Debug.LogWarning($"이미지 로드 실패: {imageName}");
        }
    }

    private void ClearCurrentSprite()
    {
        if (currentSprite != null)
        {
            DestroyImmediate(currentSprite);
            currentSprite = null;
        }
    }

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

    private void OnDestroy()
    {
        // 컴포넌트 파괴시 스프라이트 정리
        ClearCurrentSprite();
    }
}