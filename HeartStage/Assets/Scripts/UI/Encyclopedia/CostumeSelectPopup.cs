using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의상 선택 팝업 - 상의/하의/신발 탭으로 보유 의상 선택
/// 의상 클릭 시 임시 미리보기, 완료 버튼 클릭 시 실제 저장
/// </summary>
public class CostumeSelectPopup : MonoBehaviour
{
    [Header("탭 버튼")]
    [SerializeField] private Button topTabButton;
    [SerializeField] private Button pantsTabButton;
    [SerializeField] private Button shoesTabButton;

    [Header("탭 텍스트 (선택 상태 표시용)")]
    [SerializeField] private TMP_Text topTabText;
    [SerializeField] private TMP_Text pantsTabText;
    [SerializeField] private TMP_Text shoesTabText;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject costumeItemPrefab;

    [Header("버튼")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    [Header("빈 목록 안내")]
    [SerializeField] private GameObject emptyNotice;

    [Header("로딩 표시")]
    [SerializeField] private GameObject loadingIndicator;

    public bool IsOpen => gameObject.activeSelf;

    private readonly List<GameObject> _spawnedItems = new();
    private readonly List<CostumeSlotItemUI> _items = new();

    private CostumeType _currentTab = CostumeType.Top;
    private int _selectedItemId;
    private string _characterName;

    // 캐릭터 프리뷰 관련 (CharacterDetailPanel의 캐릭터 사용)
    private CostumeController _costumeController;

    // 원본 의상 저장 (닫을 때 복구용)
    private EquippedCostume _originalCostume;
    // 현재 탭별 선택된 의상 (임시)
    private int _pendingTopId;
    private int _pendingPantsId;
    private int _pendingShoesId;
    private bool _hasChanges;

    // 의상 적용 후 콜백
    public event Action OnCostumeChanged;

    private void Awake()
    {
        if (topTabButton != null)
            topTabButton.onClick.AddListener(() => SwitchTab(CostumeType.Top));
        if (pantsTabButton != null)
            pantsTabButton.onClick.AddListener(() => SwitchTab(CostumeType.Pants));
        if (shoesTabButton != null)
            shoesTabButton.onClick.AddListener(() => SwitchTab(CostumeType.Shoes));

        if (applyButton != null)
            applyButton.onClick.AddListener(OnClickApply);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    /// <param name="characterName">캐릭터 이름 (의상 저장에 사용)</param>
    /// <param name="initialTab">초기 탭</param>
    /// <param name="costumeController">미리보기용 CostumeController (CharacterDetailPanel의 캐릭터)</param>
    public void Open(string characterName, CostumeType initialTab = CostumeType.Top, CostumeController costumeController = null)
    {
        _characterName = characterName;
        _currentTab = initialTab;
        _selectedItemId = 0;
        _hasChanges = false;
        _costumeController = costumeController;

        // 원본 의상 저장
        SaveOriginalCostume();

        gameObject.SetActive(true);
        UpdateTabVisuals();
        UpdateApplyButtonState();
        RebuildListAsync().Forget();
    }

    private void SaveOriginalCostume()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData != null && !string.IsNullOrEmpty(_characterName))
        {
            if (saveData.equippedCostumeByChar.TryGetValue(_characterName, out var costume))
            {
                _originalCostume = new EquippedCostume(costume.topItemId, costume.pantsItemId, costume.shoesItemId);
                _pendingTopId = costume.topItemId;
                _pendingPantsId = costume.pantsItemId;
                _pendingShoesId = costume.shoesItemId;
            }
            else
            {
                _originalCostume = new EquippedCostume();
                _pendingTopId = 0;
                _pendingPantsId = 0;
                _pendingShoesId = 0;
            }
        }
    }

    public void Close()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        // 변경 사항 적용 안 했으면 원래 의상으로 복구
        if (_hasChanges)
        {
            RestoreOriginalCostume();
        }

        _costumeController = null;
        gameObject.SetActive(false);
    }

    private void RestoreOriginalCostume()
    {
        if (_originalCostume == null)
            return;

        // 캐릭터 의상 복구 (저장된 의상으로)
        if (_costumeController != null)
        {
            _costumeController.LoadEquippedCostume();
        }
    }

    private void SwitchTab(CostumeType newTab)
    {
        if (_currentTab == newTab)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        _currentTab = newTab;
        _selectedItemId = 0;
        UpdateTabVisuals();
        RebuildListAsync().Forget();
    }

    private void UpdateTabVisuals()
    {
        // 탭 버튼 색상/텍스트 업데이트
        SetTabActive(topTabText, _currentTab == CostumeType.Top);
        SetTabActive(pantsTabText, _currentTab == CostumeType.Pants);
        SetTabActive(shoesTabText, _currentTab == CostumeType.Shoes);
    }

    private void SetTabActive(TMP_Text tabText, bool active)
    {
        if (tabText == null) return;
        tabText.color = active ? Color.white : new Color(1f, 1f, 1f, 0.5f);
    }

    private async UniTaskVoid RebuildListAsync()
    {
        // 로딩 표시
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        if (emptyNotice != null)
            emptyNotice.SetActive(false);

        // 기존 아이템 정리
        foreach (var go in _spawnedItems)
        {
            if (go != null)
                Destroy(go);
        }
        _spawnedItems.Clear();
        _items.Clear();

        // 보유 의상 가져오기
        var ownedCostumes = CostumeHelper.GetOwnedCostumes(_currentTab);

        // 프리로드 필요시 실행
        if (CostumeHelper.NeedsPreload(_currentTab))
        {
            await CostumeHelper.PreloadOwnedCostumes(_currentTab);
        }

        // 현재 장착 의상 확인
        int equippedItemId = GetEquippedItemId(_currentTab);

        if (ownedCostumes.Count == 0)
        {
            if (emptyNotice != null)
                emptyNotice.SetActive(true);
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
            UpdateApplyButtonState();
            return;
        }

        // 아이템 생성
        foreach (var itemId in ownedCostumes)
        {
            var itemData = DataTableManager.ItemTable?.Get(itemId);
            string costumeName = itemData?.item_name ?? $"의상 {itemId}";
            string iconKey = itemData?.prefab;

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(iconKey))
            {
                sprite = ResourceManager.Instance.GetSprite(iconKey);
            }

            // 스프라이트가 없으면 첫 번째 의상 스프라이트 사용
            if (sprite == null)
            {
                int spriteId = CostumeItemID.GetSpriteId(itemId);
                string address = CostumeHelper.GetSpriteAddress(_currentTab, spriteId, 0);
                sprite = await CostumeHelper.LoadSprite(address);
            }

            var go = Instantiate(costumeItemPrefab, contentRoot);
            go.SetActive(true);
            _spawnedItems.Add(go);

            var item = go.GetComponent<CostumeSlotItemUI>();
            if (item != null)
            {
                bool isEquipped = itemId == equippedItemId;
                item.Setup(this, itemId, sprite, costumeName, isEquipped);
                _items.Add(item);

                // 장착 중인 의상은 자동 선택
                if (isEquipped)
                {
                    _selectedItemId = itemId;
                    item.SetSelected(true);
                }
            }
        }

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        UpdateApplyButtonState();
    }

    private int GetEquippedItemId(CostumeType type)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || string.IsNullOrEmpty(_characterName))
            return 0;

        if (!saveData.equippedCostumeByChar.TryGetValue(_characterName, out var costume))
            return 0;

        return type switch
        {
            CostumeType.Top => costume.topItemId,
            CostumeType.Pants => costume.pantsItemId,
            CostumeType.Shoes => costume.shoesItemId,
            _ => 0
        };
    }

    public void OnClickItem(CostumeSlotItemUI clickedItem)
    {
        _selectedItemId = clickedItem.ItemId;

        foreach (var item in _items)
        {
            item.SetSelected(item == clickedItem);
        }

        // 현재 탭의 pending 값 업데이트
        switch (_currentTab)
        {
            case CostumeType.Top:
                _pendingTopId = _selectedItemId;
                break;
            case CostumeType.Pants:
                _pendingPantsId = _selectedItemId;
                break;
            case CostumeType.Shoes:
                _pendingShoesId = _selectedItemId;
                break;
        }

        // 변경 여부 체크
        _hasChanges = (_pendingTopId != _originalCostume?.topItemId) ||
                      (_pendingPantsId != _originalCostume?.pantsItemId) ||
                      (_pendingShoesId != _originalCostume?.shoesItemId);

        // 프리뷰 캐릭터에 임시 적용
        ApplyPreviewCostume();
        UpdateApplyButtonState();
    }

    private void ApplyPreviewCostume()
    {
        if (_costumeController == null)
            return;

        // CostumeController에 임시 의상 적용
        _costumeController.ApplyTemporaryCostume(_pendingTopId, _pendingPantsId, _pendingShoesId);
    }

    private void UpdateApplyButtonState()
    {
        // 완료 버튼: 변경 사항이 있을 때만 활성화
        if (applyButton != null)
        {
            applyButton.interactable = _hasChanges;
        }
    }

    private void OnClickApply()
    {
        if (!_hasChanges)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        ApplyAllPendingCostumesAsync().Forget();
    }

    private async UniTaskVoid ApplyAllPendingCostumesAsync()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || string.IsNullOrEmpty(_characterName))
            return;

        // 모든 pending 의상을 한 번에 저장
        var costume = new EquippedCostume(_pendingTopId, _pendingPantsId, _pendingShoesId);
        saveData.equippedCostumeByChar[_characterName] = costume;

        await SaveLoadManager.SaveToServer();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Enhance);

        // 변경 사항 적용 완료 표시
        _hasChanges = false;

        OnCostumeChanged?.Invoke();
        ToastUI.Show("의상이 적용되었습니다.");

        _costumeController = null;
        gameObject.SetActive(false);
    }
}
