using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class Gacha5TryResultUI : GenericWindow
{
    [Header("5개 슬롯 (고정 배치)")]
    [SerializeField] private Image[] resultImages;
    [SerializeField] private TextMeshProUGUI[] nameTexts;
    [SerializeField] private TextMeshProUGUI[] countTexts;
    [SerializeField] private Transform[] slotTransforms; // 슬롯 부모 Transform (애니메이션용)

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retryButton;

    [Header("애니메이션 설정")]
    [SerializeField] private float popInterval = 0.15f; // 각 슬롯 등장 간격
    [SerializeField] private float popDuration = 0.3f; // 뿅 애니메이션 시간
    [SerializeField] private float convertDelay = 0.3f; // 변환 대기 시간
    [SerializeField] private float convertDuration = 0.25f; // 변환 애니메이션 시간

    private const int SlotCount = 5;
    private Sequence _popSequence;
    private List<GachaResult> _currentResults;
    private bool _isAnimating;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public override void Open()
    {
        base.Open();

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Gacha_Result);

        if (GachaUI.gachaFiveResultReceiver != null && GachaUI.gachaFiveResultReceiver.Count > 0)
        {
            _currentResults = new List<GachaResult>(GachaUI.gachaFiveResultReceiver);
            GachaUI.gachaFiveResultReceiver = null;
            PlayResultAnimation();
        }
    }

    public override void Close()
    {
        KillAnimations();
        base.Close();
    }

    private void OnDestroy()
    {
        KillAnimations();
    }

    private void KillAnimations()
    {
        _popSequence?.Kill();
        _popSequence = null;
        _isAnimating = false;
    }

    /// <summary>
    /// 터치 시 애니메이션 스킵
    /// </summary>
    public void OnScreenTap()
    {
        if (_isAnimating && _popSequence != null && _popSequence.IsActive())
        {
            _popSequence.Complete();
        }
    }

    private void PlayResultAnimation()
    {
        KillAnimations();
        _isAnimating = true;

        // 모든 슬롯 초기화 (스케일 0, 알파 0)
        for (int i = 0; i < SlotCount; i++)
        {
            if (i < slotTransforms.Length && slotTransforms[i] != null)
            {
                slotTransforms[i].localScale = Vector3.zero;
                var cg = slotTransforms[i].GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
            ClearSlot(i);
        }

        _popSequence = DOTween.Sequence();

        // 1단계: 순차적으로 등장 (조각/아이템 이미지로)
        float totalPopTime = 0f;
        for (int i = 0; i < SlotCount; i++)
        {
            int index = i;
            float delay = i * popInterval;

            if (index < _currentResults.Count)
            {
                var result = _currentResults[index];

                // 데이터 설정 콜백 (조각/아이템 먼저 표시)
                _popSequence.InsertCallback(delay, () =>
                {
                    DisplaySlotInitial(index, result);
                });

                // 스케일 + 페이드 애니메이션
                if (index < slotTransforms.Length && slotTransforms[index] != null)
                {
                    _popSequence.Insert(delay, slotTransforms[index]
                        .DOScale(Vector3.one, popDuration)
                        .SetEase(Ease.OutBack));

                    var canvasGroup = slotTransforms[index].GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        _popSequence.Insert(delay, canvasGroup
                            .DOFade(1f, popDuration)
                            .SetEase(Ease.OutQuad));
                    }
                }

                totalPopTime = delay + popDuration;
            }
        }

        // 2단계: 모두 등장 후 4등급 변환 애니메이션
        float convertStartTime = totalPopTime + convertDelay;
        for (int i = 0; i < _currentResults.Count && i < SlotCount; i++)
        {
            int index = i;
            var result = _currentResults[index];

            if (result.isMaxRank)
            {
                _popSequence.InsertCallback(convertStartTime, () =>
                {
                    PlayConvertAnimation(index, result);
                });
            }
        }

        _popSequence.OnComplete(() =>
        {
            _isAnimating = false;
        });
    }

    /// <summary>
    /// 초기 표시 (조각/아이템 이미지로)
    /// </summary>
    private void DisplaySlotInitial(int index, GachaResult result)
    {
        var characterData = result.characterData;
        var gachaData = result.gachaData;

        if (characterData != null)
        {
            if (result.isDuplicate && gachaData.Gacha_have > 0)
            {
                // 중복 - 조각 이미지 표시
                var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_have);
                if (itemData != null)
                {
                    SetSlotImage(index, itemData.prefab);
                    SetSlotName(index, itemData.item_name);
                    SetSlotCount(index, result.isMaxRank ? result.trainingPointAmount : gachaData.Gacha_have_amount);
                    return;
                }
            }

            // 새 캐릭터
            SetSlotImage(index, characterData.card_imageName);
            SetSlotName(index, characterData.char_name);
            SetSlotCount(index, 0);
        }
        else
        {
            // 아이템 (조각 포함)
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetSlotImage(index, itemData.prefab);
                SetSlotName(index, itemData.item_name);
                SetSlotCount(index, result.isMaxRank ? result.trainingPointAmount : gachaData.Gacha_item_amount);
            }
        }
    }

    /// <summary>
    /// 4등급 변환 애니메이션 (조각 → 트레이닝 포인트)
    /// </summary>
    private void PlayConvertAnimation(int index, GachaResult result)
    {
        if (index >= resultImages.Length || resultImages[index] == null)
            return;

        var image = resultImages[index];
        var trainingPointData = DataTableManager.ItemTable.Get(ItemID.TrainingPoint);
        if (trainingPointData == null) return;

        // 축소 → 이미지 변경 → 확대
        Sequence convertSeq = DOTween.Sequence();
        convertSeq.Append(image.transform.DOScale(0.5f, convertDuration * 0.5f).SetEase(Ease.InBack));
        convertSeq.AppendCallback(() =>
        {
            image.sprite = ResourceManager.Instance.GetSprite(trainingPointData.prefab);
            SetSlotName(index, trainingPointData.item_name);
        });
        convertSeq.Append(image.transform.DOScale(1f, convertDuration * 0.5f).SetEase(Ease.OutBack));
    }

    private void DisplaySlotFinal(int index, GachaResult result)
    {
        var characterData = result.characterData;
        var gachaData = result.gachaData;

        // 4등급이면 무조건 트레이닝 포인트 표시
        if (result.isMaxRank)
        {
            var trainingPointData = DataTableManager.ItemTable.Get(ItemID.TrainingPoint);
            if (trainingPointData != null)
            {
                SetSlotImage(index, trainingPointData.prefab);
                SetSlotName(index, trainingPointData.item_name);
                SetSlotCount(index, result.trainingPointAmount);
                return;
            }
        }

        if (characterData != null)
        {
            if (result.isDuplicate && gachaData.Gacha_have > 0)
            {
                // 조각 표시
                var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_have);
                if (itemData != null)
                {
                    SetSlotImage(index, itemData.prefab);
                    SetSlotName(index, itemData.item_name);
                    SetSlotCount(index, gachaData.Gacha_have_amount);
                    return;
                }
            }

            // 새 캐릭터
            SetSlotImage(index, characterData.card_imageName);
            SetSlotName(index, characterData.char_name);
            SetSlotCount(index, 0);
        }
        else
        {
            // 아이템
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetSlotImage(index, itemData.prefab);
                SetSlotName(index, itemData.item_name);
                SetSlotCount(index, gachaData.Gacha_item_amount);
            }
        }
    }

    private void ClearSlot(int index)
    {
        if (index < resultImages.Length && resultImages[index] != null)
        {
            resultImages[index].sprite = null;
        }
        SetSlotName(index, "");
        SetSlotCount(index, 0);
    }

    private void SetSlotImage(int index, string imageName)
    {
        if (index >= resultImages.Length || resultImages[index] == null)
            return;

        if (string.IsNullOrEmpty(imageName))
            return;

        resultImages[index].sprite = ResourceManager.Instance.GetSprite(imageName);
    }

    private void SetSlotName(int index, string name)
    {
        if (index >= nameTexts.Length || nameTexts[index] == null)
            return;

        nameTexts[index].text = name;
    }

    private void SetSlotCount(int index, int count)
    {
        if (index >= countTexts.Length || countTexts[index] == null)
            return;

        if (count > 0)
        {
            countTexts[index].gameObject.SetActive(true);
            countTexts[index].text = $"x{count}";
        }
        else
        {
            countTexts[index].gameObject.SetActive(false);
        }
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    private void OnRetryButtonClicked()
    {
        var gachaResults = GachaManager.Instance.DrawGachaFiveTimes(2);

        if (gachaResults != null && gachaResults.Count > 0)
        {
            _currentResults = new List<GachaResult>(gachaResults);
            PlayResultAnimation();
        }
        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.GachaCancel);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }
}
