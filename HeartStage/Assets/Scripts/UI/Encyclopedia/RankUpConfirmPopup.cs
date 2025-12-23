using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 등급 강화 확인 팝업
/// 현재 등급 → 다음 등급 비교 및 강화 실행
/// </summary>
public class RankUpConfirmPopup : MonoBehaviour
{
    [Header("팝업 컨트롤")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;  // 배경 클릭으로 닫기

    [Header("현재 등급 정보")]
    [SerializeField] private TextMeshProUGUI currentRankNameText;  // "에이스 연습생"
    [SerializeField] private Image[] currentRankMics;              // 마이크 아이콘들
    [SerializeField] private Sprite micOnSprite;
    [SerializeField] private Sprite micOffSprite;
    [SerializeField] private TextMeshProUGUI currentSkillInfoText; // 스킬 설명
    [SerializeField] private Image currentSkillIcon;               // 스킬 아이콘
    [SerializeField] private PassiveTileDisplay currentPassiveTile; // 패시브 타일 표시

    [Header("다음 등급 정보")]
    [SerializeField] private TextMeshProUGUI nextRankNameText;     // "신인 아이돌"
    [SerializeField] private Image[] nextRankMics;                 // 마이크 아이콘들
    [SerializeField] private TextMeshProUGUI nextSkillInfoText;    // 스킬 설명 (변경 수치 빨간색)
    [SerializeField] private Image nextSkillIcon;                  // 스킬 아이콘
    [SerializeField] private PassiveTileDisplay nextPassiveTile;   // 패시브 타일 표시

    [Header("조각 게이지")]
    [SerializeField] private Image pieceIcon;                      // 조각 아이콘
    [SerializeField] private TextMeshProUGUI pieceCountText;       // "13/10"
    [SerializeField] private Slider pieceGaugeSlider;              // 게이지 바

    [Header("강화 버튼")]
    [SerializeField] private Button rankUpButton;
    [SerializeField] private TextMeshProUGUI rankUpButtonText;

    // 콜백
    public event Action OnRankUpCompleted;

    // 현재 데이터
    private int _currentCharId;
    private int _nextCharId;
    private RankUpData _rankUpData;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(Close);

        if (rankUpButton != null)
            rankUpButton.onClick.AddListener(OnRankUpButtonClick);
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    public void Open(int charId)
    {
        _currentCharId = charId;

        var charData = DataTableManager.CharacterTable.Get(charId);
        if (charData == null)
        {
            Debug.LogWarning($"[RankUpConfirmPopup] 캐릭터 데이터 없음: {charId}");
            return;
        }

        _rankUpData = DataTableManager.RankUpTable.Get(charId);
        if (_rankUpData == null)
        {
            Debug.LogWarning($"[RankUpConfirmPopup] 랭크업 데이터 없음: {charId}");
            return;
        }

        _nextCharId = _rankUpData.Upgrade_char;
        var nextCharData = DataTableManager.CharacterTable.Get(_nextCharId);
        if (nextCharData == null)
        {
            Debug.LogWarning($"[RankUpConfirmPopup] 다음 캐릭터 데이터 없음: {_nextCharId}");
            return;
        }

        // 현재/다음 등급 정보 설정 (변경되는 스킬 기준)
        SetRankInfo(charData, nextCharData);

        // 조각 게이지 설정
        SetPieceGauge();

        // 강화 버튼 상태 설정
        UpdateRankUpButton();

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void Close()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 변경되는 스킬 찾기 (현재 vs 다음 스킬 ID 비교)
    /// </summary>
    private (int currentSkillId, int nextSkillId) FindChangedSkill(CharacterCSVData currentCharData, CharacterCSVData nextCharData)
    {
        var currentSkillIds = DataTableManager.CharacterTable.GetSkillIds(currentCharData.char_id);
        var nextSkillIds = DataTableManager.CharacterTable.GetSkillIds(nextCharData.char_id);

        if (currentSkillIds == null || nextSkillIds == null)
            return (0, 0);

        // 스킬 ID 비교해서 변경된 것 찾기
        int count = Mathf.Min(currentSkillIds.Count, nextSkillIds.Count);
        for (int i = 0; i < count; i++)
        {
            if (currentSkillIds[i] != nextSkillIds[i])
            {
                return (currentSkillIds[i], nextSkillIds[i]);
            }
        }

        // 변경된 스킬이 없으면 첫 번째 스킬 반환
        int curId = currentSkillIds.Count > 0 ? currentSkillIds[0] : 0;
        int nextId = nextSkillIds.Count > 0 ? nextSkillIds[0] : 0;
        return (curId, nextId);
    }

    /// <summary>
    /// 현재/다음 등급 정보 설정 (변경되는 스킬 기준)
    /// </summary>
    private void SetRankInfo(CharacterCSVData currentCharData, CharacterCSVData nextCharData)
    {
        // 등급 이름 및 마이크
        if (currentRankNameText != null)
            currentRankNameText.text = GetRankName(currentCharData.char_rank);
        UpdateMicIcons(currentRankMics, currentCharData.char_rank);

        if (nextRankNameText != null)
            nextRankNameText.text = GetRankName(nextCharData.char_rank);
        UpdateMicIcons(nextRankMics, nextCharData.char_rank);

        // 변경되는 스킬 찾기
        var (currentSkillId, nextSkillId) = FindChangedSkill(currentCharData, nextCharData);

        // 현재 스킬 정보 표시
        if (currentSkillId > 0)
        {
            var currentSkillData = DataTableManager.SkillTable.Get(currentSkillId);
            if (currentSkillData != null)
            {
                if (currentSkillInfoText != null)
                    currentSkillInfoText.text = currentSkillData.GetFormattedInfo();

                if (currentSkillIcon != null)
                    ApplySkillIcon(currentSkillIcon, currentSkillData.icon_prefab);

                if (currentPassiveTile != null)
                    currentPassiveTile.SetPattern(currentSkillData.passive_type);
            }
        }

        // 다음 스킬 정보 표시
        if (nextSkillId > 0)
        {
            var nextSkillData = DataTableManager.SkillTable.Get(nextSkillId);
            if (nextSkillData != null)
            {
                if (nextSkillInfoText != null)
                    nextSkillInfoText.text = nextSkillData.GetFormattedInfo();

                if (nextSkillIcon != null)
                    ApplySkillIcon(nextSkillIcon, nextSkillData.icon_prefab);

                if (nextPassiveTile != null)
                    nextPassiveTile.SetPattern(nextSkillData.passive_type);
            }
        }
    }

    /// <summary>
    /// 조각 게이지 설정
    /// </summary>
    private void SetPieceGauge()
    {
        if (_rankUpData == null) return;

        int currentAmount = ItemInvenHelper.GetAmount(_rankUpData.Upgrade_ingrd_Itm1);
        int requiredAmount = _rankUpData.Ingrd_Itm1_amount;

        // 텍스트
        if (pieceCountText != null)
            pieceCountText.text = $"{currentAmount}/{requiredAmount}";

        // 게이지
        if (pieceGaugeSlider != null)
        {
            pieceGaugeSlider.maxValue = requiredAmount;
            pieceGaugeSlider.value = Mathf.Min(currentAmount, requiredAmount);
        }

        // 아이콘 (조각 아이템 이미지)
        if (pieceIcon != null)
        {
            var itemData = DataTableManager.ItemTable?.Get(_rankUpData.Upgrade_ingrd_Itm1);
            if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
            {
                var sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
                if (sprite != null)
                    pieceIcon.sprite = sprite;
            }
        }
    }

    /// <summary>
    /// 강화 버튼 상태 업데이트
    /// </summary>
    private void UpdateRankUpButton()
    {
        if (_rankUpData == null || rankUpButton == null) return;

        int currentAmount = ItemInvenHelper.GetAmount(_rankUpData.Upgrade_ingrd_Itm1);
        bool canRankUp = currentAmount >= _rankUpData.Ingrd_Itm1_amount;

        rankUpButton.interactable = canRankUp;

        if (rankUpButtonText != null)
            rankUpButtonText.text = "등급 강화";
    }

    /// <summary>
    /// 강화 버튼 클릭
    /// </summary>
    private void OnRankUpButtonClick()
    {
        if (_rankUpData == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 재료 소모
        if (!ItemInvenHelper.TryConsumeItem(_rankUpData.Upgrade_ingrd_Itm1, _rankUpData.Ingrd_Itm1_amount))
        {
            ToastUI.Show("재료가 부족합니다.");
            return;
        }

        // 캐릭터 업그레이드
        CharacterHelper.CommitUpgradeResult(_currentCharId, _nextCharId);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Enhance);

        Debug.Log($"[RankUpConfirmPopup] 랭크업 완료: {_currentCharId} → {_nextCharId}");

        // 콜백 호출
        OnRankUpCompleted?.Invoke();

        // 팝업 닫기
        Close();
    }

    /// <summary>
    /// 마이크 아이콘 업데이트
    /// </summary>
    private void UpdateMicIcons(Image[] mics, int rank)
    {
        if (mics == null) return;

        for (int i = 0; i < mics.Length; i++)
        {
            if (mics[i] == null) continue;
            bool isOn = i < rank;
            mics[i].sprite = isOn ? micOnSprite : micOffSprite;
        }
    }

    /// <summary>
    /// 스킬 아이콘 적용
    /// </summary>
    private void ApplySkillIcon(Image iconImage, string iconKey)
    {
        if (iconImage == null || string.IsNullOrEmpty(iconKey)) return;

        var sprite = ResourceManager.Instance.GetSprite(iconKey);
        if (sprite != null)
            iconImage.sprite = sprite;
    }

    /// <summary>
    /// 랭크 이름 반환
    /// </summary>
    private string GetRankName(int rank)
    {
        return rank switch
        {
            1 => "병아리 연습생",
            2 => "에이스 연습생",
            3 => "신인 아이돌",
            4 => "인기 아이돌",
            _ => ""
        };
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (backgroundButton != null)
            backgroundButton.onClick.RemoveAllListeners();

        if (rankUpButton != null)
            rankUpButton.onClick.RemoveAllListeners();
    }
}
