using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의상 선택 팝업 - 상의/하의/신발 탭으로 보유 의상 선택
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
    [SerializeField] private Button unequipButton;  // 장착 해제 버튼
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
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnClickUnequip);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    /// <param name="characterName">캐릭터 이름 (의상 저장에 사용)</param>
    /// <param name="initialTab">초기 탭</param>
    public void Open(string characterName, CostumeType initialTab = CostumeType.Top)
    {
        _characterName = characterName;
        _currentTab = initialTab;
        _selectedItemId = 0;

        gameObject.SetActive(true);
        UpdateTabVisuals();
        RebuildListAsync().Forget();
    }

    public void Close()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        gameObject.SetActive(false);
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
            UpdateButtonStates();
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

        UpdateButtonStates();
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

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        int equippedId = GetEquippedItemId(_currentTab);

        // 적용 버튼: 선택한 의상이 있고, 현재 장착 의상과 다를 때만 활성화
        if (applyButton != null)
        {
            bool canApply = _selectedItemId > 0 && _selectedItemId != equippedId;
            applyButton.interactable = canApply;
        }

        // 해제 버튼: 현재 장착 의상이 있을 때만 활성화
        if (unequipButton != null)
        {
            unequipButton.interactable = equippedId > 0;
        }
    }

    private void OnClickApply()
    {
        if (_selectedItemId <= 0)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        ApplyCostumeAsync(_selectedItemId).Forget();
    }

    private async UniTaskVoid ApplyCostumeAsync(int itemId)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || string.IsNullOrEmpty(_characterName))
            return;

        // SaveData 업데이트
        if (!saveData.equippedCostumeByChar.TryGetValue(_characterName, out var costume))
        {
            costume = new EquippedCostume();
        }

        switch (_currentTab)
        {
            case CostumeType.Top:
                costume.topItemId = itemId;
                break;
            case CostumeType.Pants:
                costume.pantsItemId = itemId;
                break;
            case CostumeType.Shoes:
                costume.shoesItemId = itemId;
                break;
        }

        saveData.equippedCostumeByChar[_characterName] = costume;
        await SaveLoadManager.SaveToServer();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Enhance);

        // 장착 표시 업데이트
        foreach (var item in _items)
        {
            item.SetEquipped(item.ItemId == itemId);
        }

        UpdateButtonStates();
        OnCostumeChanged?.Invoke();

        ToastUI.Show("의상이 적용되었습니다.");
    }

    private void OnClickUnequip()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        UnequipCostumeAsync().Forget();
    }

    private async UniTaskVoid UnequipCostumeAsync()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || string.IsNullOrEmpty(_characterName))
            return;

        if (!saveData.equippedCostumeByChar.TryGetValue(_characterName, out var costume))
            return;

        switch (_currentTab)
        {
            case CostumeType.Top:
                costume.topItemId = 0;
                break;
            case CostumeType.Pants:
                costume.pantsItemId = 0;
                break;
            case CostumeType.Shoes:
                costume.shoesItemId = 0;
                break;
        }

        saveData.equippedCostumeByChar[_characterName] = costume;
        await SaveLoadManager.SaveToServer();

        // 장착 표시 모두 해제
        foreach (var item in _items)
        {
            item.SetEquipped(false);
        }

        _selectedItemId = 0;
        foreach (var item in _items)
        {
            item.SetSelected(false);
        }

        UpdateButtonStates();
        OnCostumeChanged?.Invoke();

        ToastUI.Show("의상이 해제되었습니다.");
    }
}
