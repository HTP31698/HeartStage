using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 의상 선택 팝업 - 상의/하의/신발 탭으로 보유 의상 선택
/// 의상 클릭 시 임시 미리보기, 완료 버튼 클릭 시 실제 저장
/// </summary>
public class CostumeSelectPopup : MonoBehaviour
{
    [Header("캐릭터 프리뷰 (실시간 의상 확인)")]
    [SerializeField] private RawImage characterPreviewRawImage;  // 캐릭터 프리뷰용 RawImage

    [Header("장착 중인 의상 이미지")]
    [SerializeField] private Image topEquippedImage;
    [SerializeField] private Image pantsEquippedImage;
    [SerializeField] private Image shoesEquippedImage;

    [Header("탭 버튼")]
    [SerializeField] private Button topTabButton;
    [SerializeField] private Button pantsTabButton;
    [SerializeField] private Button shoesTabButton;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject costumeItemPrefab;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private ScrollRect scrollRect;  // Viewport 너비 계산용
    [SerializeField] private CanvasGroup listCanvasGroup;  // 로딩 중 숨김용
    [SerializeField] private int columnCount = 4;
    [SerializeField] private float cellSpacing = 10f;

    [Header("버튼")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    public bool IsOpen => _isOpen;
    private bool _isOpen;
    private int _openFrame;  // 열린 프레임 (같은 프레임에 Close 방지)

    private readonly List<GameObject> _spawnedItems = new();
    private readonly List<CostumeSlotItemUI> _items = new();

    private CostumeType _currentTab = CostumeType.Top;
    private int _selectedItemId;
    private string _characterName;

    // 캐릭터 프리뷰 관련 (CharacterDetailPanel의 캐릭터 사용)
    private CostumeController _costumeController;
    private RenderTexture _sharedRenderTexture;  // CharacterDetailPanel에서 공유받은 RenderTexture

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
    }

    private void OnDisable()
    {
        // 비활성화될 때 상태 리셋 (강제 종료 시에도 다시 열 수 있도록)
        _isOpen = false;
        NoteLoadingUI.ForceHide();
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    /// <param name="characterName">캐릭터 이름 (의상 저장에 사용)</param>
    /// <param name="initialTab">초기 탭</param>
    /// <param name="costumeController">미리보기용 CostumeController (CharacterDetailPanel의 캐릭터)</param>
    /// <param name="renderTexture">캐릭터 렌더링용 RenderTexture (CharacterDetailPanel에서 공유)</param>
    public void Open(string characterName, CostumeType initialTab = CostumeType.Top, CostumeController costumeController = null, RenderTexture renderTexture = null)
    {
        // 이미 열려있으면 무시
        if (_isOpen)
            return;

        _isOpen = true;
        _openFrame = Time.frameCount;  // 같은 프레임에 Close 방지용

        // 로딩 표시 (가장 먼저!)
        NoteLoadingUI.Show();

        _characterName = characterName;
        _currentTab = initialTab;
        _selectedItemId = 0;
        _hasChanges = false;
        _costumeController = costumeController;
        _sharedRenderTexture = renderTexture;

        // 팝업 먼저 활성화 (자식 오브젝트들이 활성화되려면 부모가 먼저 활성화되어야 함)
        gameObject.SetActive(true);

        // 캐릭터 프리뷰 RawImage에 RenderTexture 연결
        if (characterPreviewRawImage != null && _sharedRenderTexture != null)
        {
            characterPreviewRawImage.texture = _sharedRenderTexture;
            characterPreviewRawImage.gameObject.SetActive(true);
        }
        else if (characterPreviewRawImage != null)
        {
            characterPreviewRawImage.gameObject.SetActive(false);
        }

        // 원본 의상 저장
        SaveOriginalCostume();

        // 리스트 숨김 (로딩 중)
        if (listCanvasGroup != null)
            listCanvasGroup.alpha = 0;

        // 공용 딤 배경 표시 및 이벤트 구독
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ShowDimManual();
            WindowManager.Instance.OnDimClicked += OnDimClicked;
        }

        AdjustGridCellSize();
        UpdateApplyButtonState();
        RefreshEquippedImagesAsync().Forget();
        RebuildListAsync().Forget();
    }

    /// <summary>
    /// 화면 크기에 맞게 그리드 셀 크기 자동 조절
    /// </summary>
    private void AdjustGridCellSize()
    {
        if (gridLayout == null)
            return;

        // Viewport 너비 사용 (ScrollRect가 있으면)
        float containerWidth = 0f;
        if (scrollRect != null && scrollRect.viewport != null)
        {
            containerWidth = scrollRect.viewport.rect.width;
        }
        else if (contentRoot is RectTransform contentRect)
        {
            containerWidth = contentRect.rect.width;
        }

        if (containerWidth <= 0)
        {
            Debug.LogWarning($"[CostumeSelectPopup] AdjustGridCellSize: containerWidth is {containerWidth}, skipping");
            return;
        }

        // 컨테이너 너비에서 패딩과 스페이싱 제외 후 셀 크기 계산
        float totalSpacing = cellSpacing * (columnCount - 1);
        float padding = gridLayout.padding.left + gridLayout.padding.right;
        float availableWidth = containerWidth - totalSpacing - padding;
        float cellSize = availableWidth / columnCount;

        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnCount;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleLeft;
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

    /// <summary>
    /// 딤 클릭으로 닫힐 때 호출
    /// </summary>
    private void OnDimClicked()
    {
        // 열린 프레임과 같은 프레임에서는 닫지 않음 (터치 중복 방지)
        if (Time.frameCount == _openFrame)
            return;

        if (WindowManager.Instance != null)
            WindowManager.Instance.OnDimClicked -= OnDimClicked;

        // 변경 사항 적용 안 했으면 원래 의상으로 복구
        if (_hasChanges)
            RestoreOriginalCostume();

        _isOpen = false;
        _costumeController = null;
        gameObject.SetActive(false);
    }

    public void Close()
    {
        // 열린 프레임과 같은 프레임에서는 닫지 않음 (터치 중복 방지)
        if (Time.frameCount == _openFrame)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        // 변경 사항 적용 안 했으면 원래 의상으로 복구
        if (_hasChanges)
            RestoreOriginalCostume();

        // 딤 배경 숨기기 및 이벤트 해제
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OnDimClicked -= OnDimClicked;
            WindowManager.Instance.HideDimManual();
        }

        _isOpen = false;
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
        RebuildListAsync().Forget();
    }

    private async UniTaskVoid RebuildListAsync()
    {
        // 한 프레임 대기 (UI 업데이트 보장)
        await UniTask.Yield();

        try
        {
            // 기존 아이템 정리
            foreach (var go in _spawnedItems)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedItems.Clear();
            _items.Clear();

            // 프리팹/contentRoot 검증
            if (costumeItemPrefab == null)
            {
                Debug.LogError("[CostumeSelectPopup] costumeItemPrefab is null! Assign it in Inspector.");
                return;
            }
            if (contentRoot == null)
            {
                Debug.LogError("[CostumeSelectPopup] contentRoot is null! Assign it in Inspector.");
                return;
            }

            // 보유 의상 가져오기
            var ownedCostumes = CostumeHelper.GetOwnedCostumes(_currentTab);
            if (ownedCostumes.Count == 0)
                return;

            // 현재 장착 의상 확인
            int equippedItemId = GetEquippedItemId(_currentTab);

            // 모든 아이템 동기적으로 생성
            for (int i = 0; i < ownedCostumes.Count; i++)
            {
                int itemId = ownedCostumes[i];

                var itemData = DataTableManager.ItemTable?.Get(itemId);
                string costumeName = itemData?.item_name ?? $"의상 {itemId}";
                string iconKey = itemData?.prefab;

                // 동기적으로 스프라이트 가져오기 (ResourceManager에서만)
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(iconKey))
                    sprite = ResourceManager.Instance.GetSprite(iconKey);

                var go = Instantiate(costumeItemPrefab, contentRoot);
                go.SetActive(true);
                _spawnedItems.Add(go);

                var item = go.GetComponent<CostumeSlotItemUI>();
                if (item != null)
                {
                    bool isEquipped = itemId == equippedItemId;
                    item.Setup(this, itemId, sprite, costumeName, isEquipped);
                    _items.Add(item);

                    if (isEquipped)
                    {
                        _selectedItemId = itemId;
                        item.SetSelected(true);
                    }
                }
            }

            // 레이아웃 강제 갱신
            if (contentRoot is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // 스프라이트가 없는 아이템들 비동기 로드 (백그라운드)
            _ = LoadMissingSpritesAsync(ownedCostumes);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CostumeSelectPopup] RebuildListAsync error: {ex.Message}");
        }
        finally
        {
            // 리스트 표시
            if (listCanvasGroup != null)
                listCanvasGroup.alpha = 1;

            NoteLoadingUI.Hide();
            UpdateApplyButtonState();
        }
    }

    /// <summary>
    /// 스프라이트가 없는 아이템들의 스프라이트를 백그라운드에서 로드
    /// </summary>
    private async UniTask LoadMissingSpritesAsync(List<int> itemIds)
    {
        for (int i = 0; i < itemIds.Count && i < _items.Count; i++)
        {
            var item = _items[i];
            int itemId = itemIds[i];

            // 이미 스프라이트가 있으면 스킵
            var itemData = DataTableManager.ItemTable?.Get(itemId);
            if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
            {
                var existingSprite = ResourceManager.Instance.GetSprite(itemData.prefab);
                if (existingSprite != null)
                    continue;
            }

            // Addressable에서 스프라이트 로드
            int spriteId = CostumeItemID.GetSpriteId(itemId);
            if (spriteId > 0)
            {
                string address = CostumeHelper.GetSpriteAddress(_currentTab, spriteId, 0);
                var sprite = await CostumeHelper.LoadSprite(address);
                if (sprite != null && item != null)
                    item.SetSprite(sprite);
            }
        }
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
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_ChangeCloth);

        _selectedItemId = clickedItem.ItemId;

        // 선택 마크 + 장착 마크 업데이트
        foreach (var item in _items)
        {
            bool isClicked = item == clickedItem;
            item.SetSelected(isClicked);
            item.SetEquipped(isClicked);
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
        RefreshEquippedImagesAsync().Forget();
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

    /// <summary>
    /// 상단 장착 의상 이미지 새로고침
    /// </summary>
    private async UniTaskVoid RefreshEquippedImagesAsync()
    {
        await UpdateEquippedSlot(topEquippedImage, CostumeType.Top, _pendingTopId);
        await UpdateEquippedSlot(pantsEquippedImage, CostumeType.Pants, _pendingPantsId);
        await UpdateEquippedSlot(shoesEquippedImage, CostumeType.Shoes, _pendingShoesId);
    }

    private async UniTask UpdateEquippedSlot(Image slotImage, CostumeType type, int itemId)
    {
        if (slotImage == null)
            return;

        if (itemId <= 0)
        {
            slotImage.sprite = null;
            slotImage.color = new Color(1f, 1f, 1f, 0.3f);
            return;
        }

        // ItemTable에서 아이콘 가져오기
        var itemData = DataTableManager.ItemTable?.Get(itemId);
        if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
        {
            var sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
            if (sprite != null)
            {
                slotImage.sprite = sprite;
                slotImage.color = Color.white;
                return;
            }
        }

        // 없으면 첫 번째 의상 스프라이트 사용
        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId > 0)
        {
            string address = CostumeHelper.GetSpriteAddress(type, spriteId, 0);
            var sprite = await CostumeHelper.LoadSprite(address);
            if (sprite != null)
            {
                slotImage.sprite = sprite;
                slotImage.color = Color.white;
                return;
            }
        }

        slotImage.sprite = null;
        slotImage.color = new Color(1f, 1f, 1f, 0.3f);
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

        // 딤 배경 숨기기 및 이벤트 해제
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OnDimClicked -= OnDimClicked;
            WindowManager.Instance.HideDimManual();
        }

        _isOpen = false;
        _costumeController = null;
        gameObject.SetActive(false);
    }
}
