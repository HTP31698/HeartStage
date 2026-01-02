using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaResultUI : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private CanvasGroup imageCanvasGroup; // 이미지 페이드용

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button reTryButton;

    [Header("애니메이션 설정")]
    [SerializeField] private float popDuration = 0.3f; // 뿅 애니메이션 시간
    [SerializeField] private float convertDelay = 0.5f;
    [SerializeField] private float convertDuration = 0.25f;

    private GachaResult gachaResult;
    private Sprite currentSprite;
    private Sequence _popSequence;
    private Sequence _convertSequence;

    protected override void Awake()
    {
        base.Awake();
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
        KillAnimations();
        base.Close();
    }

    private void KillAnimations()
    {
        _popSequence?.Kill();
        _popSequence = null;
        _convertSequence?.Kill();
        _convertSequence = null;
    }

    public void SetGachaResult(GachaResult result)
    {
        gachaResult = result;
    }

    private void DisPlayResult()
    {
        // 기존 애니메이션 정리
        KillAnimations();

        // 초기 상태: 스케일 0, 알파 0
        if (characterImage != null)
        {
            characterImage.transform.localScale = Vector3.zero;
            if (imageCanvasGroup != null)
            {
                imageCanvasGroup.alpha = 0f;
            }
        }

        var characterData = gachaResult.characterData;
        var gachaData = gachaResult.gachaData;
        bool needConvert = false;

        if (characterData != null)
        {
            if (gachaData.Gacha_have > 0 && gachaResult.isDuplicate)
            {
                var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_have);
                if (itemData != null)
                {
                    SetImage(itemData.prefab);
                    SetCharacterNameText(itemData.item_name);
                    SetItemCountText(gachaResult.isMaxRank ? gachaResult.trainingPointAmount : gachaData.Gacha_have_amount);
                    needConvert = gachaResult.isMaxRank;
                    PlayPopAnimation(needConvert);
                    return;
                }
            }

            SetImage(characterData.card_imageName);
            SetCharacterNameText(characterData.char_name);
            SetItemCountText(0);  // 캐릭터는 개수 표시 안함
            PlayPopAnimation(false);
        }
        else
        {
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetImage(itemData.prefab);
                SetCharacterNameText(itemData.item_name);
                SetItemCountText(gachaResult.isMaxRank ? gachaResult.trainingPointAmount : gachaData.Gacha_item_amount);
                needConvert = gachaResult.isMaxRank;
                PlayPopAnimation(needConvert);
            }
            else
            {
                SetCharacterNameText($"아이템 ID: {gachaData.Gacha_item}");
                SetItemCountText(0);
                PlayPopAnimation(false);
            }
        }
    }

    /// <summary>
    /// 페이드 인 + 뿅 등장 애니메이션
    /// </summary>
    private void PlayPopAnimation(bool triggerConvert)
    {
        if (characterImage == null) return;

        _popSequence = DOTween.Sequence();

        // 스케일 + 페이드 동시 애니메이션
        _popSequence.Append(characterImage.transform
            .DOScale(Vector3.one, popDuration)
            .SetEase(Ease.OutBack));

        if (imageCanvasGroup != null)
        {
            _popSequence.Join(imageCanvasGroup
                .DOFade(1f, popDuration)
                .SetEase(Ease.OutQuad));
        }

        // 등장 후 4등급이면 변환 애니메이션 트리거
        if (triggerConvert)
        {
            _popSequence.OnComplete(() =>
            {
                PlayConvertAnimation();
            });
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

    private void PlayConvertAnimation()
    {
        if (characterImage == null) return;

        var trainingPointData = DataTableManager.ItemTable.Get(ItemID.TrainingPoint);
        if (trainingPointData == null) return;

        _convertSequence = DOTween.Sequence();

        // 딜레이 후 축소 → 이미지 변경 → 확대
        _convertSequence.AppendInterval(convertDelay);
        _convertSequence.Append(characterImage.transform.DOScale(0.5f, convertDuration * 0.5f).SetEase(Ease.InBack));
        _convertSequence.AppendCallback(() =>
        {
            characterImage.sprite = ResourceManager.Instance.GetSprite(trainingPointData.prefab);
            SetCharacterNameText(trainingPointData.item_name);
        });
        _convertSequence.Append(characterImage.transform.DOScale(1f, convertDuration * 0.5f).SetEase(Ease.OutBack));
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
        KillAnimations();
    }
}