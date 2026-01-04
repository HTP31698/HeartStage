using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포토카드 선택 팝업 - 캐릭터별 포토카드 선택
/// 보유 카드: 클릭 가능, 체크마크로 선택 표시
/// 미보유 카드: 잠금 표시, 클릭 불가
/// </summary>
public class PhotocardSelectPopup : MonoBehaviour
{
    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject photocardItemPrefab;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private CanvasGroup listCanvasGroup;
    [SerializeField] private int columnCount = 2;  // 한 줄에 2개
    [SerializeField] private float cellSpacing = 10f;
    [SerializeField] private float cellAspectRatio = 1.5f;  // 3:2 비율 (height = width * 1.5)

    [Header("버튼")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;  // 완료 버튼

    [Header("정보 텍스트")]
    [SerializeField] private TMPro.TMP_Text titleText;  // "포토카드 선택 (2/5)" 형태

    public bool IsOpen => _isOpen;
    private bool _isOpen;
    private int _openFrame;

    private readonly List<GameObject> _spawnedItems = new();
    private readonly List<PhotocardSlotItemUI> _items = new();

    private string _charCode;
    private int _equippedItemId;      // 현재 실제 장착된 ID
    private int _pendingSelectedId;   // 선택 대기 중인 ID (확인 전)

    // 포토카드 변경 후 콜백
    public event Action OnPhotocardChanged;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnDisable()
    {
        _isOpen = false;
        NoteLoadingUI.ForceHide();
    }

    /// <summary>
    /// 팝업 열기
    /// </summary>
    /// <param name="charCode">캐릭터 코드 (예: "0101")</param>
    public void Open(string charCode)
    {
        if (_isOpen) return;

        _isOpen = true;
        _openFrame = Time.frameCount;

        NoteLoadingUI.Show();

        _charCode = charCode;
        _equippedItemId = PhotocardHelper.GetEquippedPhotocardId(charCode);
        _pendingSelectedId = _equippedItemId;  // 선택 대기 ID 초기화

        gameObject.SetActive(true);

        // 리스트 숨김 (로딩 중)
        if (listCanvasGroup != null)
            listCanvasGroup.alpha = 0;

        // 공용 딤 배경 표시
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ShowDimManual();
            WindowManager.Instance.OnDimClicked += OnDimClicked;
        }

        RebuildListAsync().Forget();
    }

    /// <summary>
    /// 화면 크기에 맞게 그리드 셀 크기 자동 조절 (3:2 비율)
    /// </summary>
    private void AdjustGridCellSize()
    {
        if (gridLayout == null) return;

        float containerWidth = 0f;
        if (scrollRect != null && scrollRect.viewport != null)
        {
            containerWidth = scrollRect.viewport.rect.width;
        }
        else if (contentRoot is RectTransform contentRect)
        {
            containerWidth = contentRect.rect.width;
        }

        if (containerWidth <= 0) return;

        float totalSpacing = cellSpacing * (columnCount - 1);
        float padding = gridLayout.padding.left + gridLayout.padding.right;
        float availableWidth = containerWidth - totalSpacing - padding;
        float cellWidth = availableWidth / columnCount;
        float cellHeight = cellWidth * cellAspectRatio;  // 3:2 비율

        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnCount;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
    }

    private void OnDimClicked()
    {
        if (Time.frameCount == _openFrame) return;

        if (WindowManager.Instance != null)
            WindowManager.Instance.OnDimClicked -= OnDimClicked;

        _isOpen = false;
        gameObject.SetActive(false);
    }

    public void Close()
    {
        if (Time.frameCount == _openFrame) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OnDimClicked -= OnDimClicked;
            WindowManager.Instance.HideDimManual();
        }

        _isOpen = false;
        gameObject.SetActive(false);
    }

    private async UniTaskVoid RebuildListAsync()
    {
        await UniTask.Yield();

        try
        {
            // 레이아웃 강제 갱신 후 그리드 셀 크기 조절
            Canvas.ForceUpdateCanvases();
            AdjustGridCellSize();

            // 기존 아이템 정리
            foreach (var go in _spawnedItems)
            {
                if (go != null) Destroy(go);
            }
            _spawnedItems.Clear();
            _items.Clear();

            if (photocardItemPrefab == null || contentRoot == null)
            {
                Debug.LogError("[PhotocardSelectPopup] Prefab or ContentRoot is null!");
                return;
            }

            // 해당 캐릭터의 모든 포토카드 가져오기
            var allPhotocards = PhotocardHelper.GetAllPhotocards(_charCode);
            if (allPhotocards.Count == 0)
            {
                UpdateTitleText(0, 0);
                return;
            }

            int ownedCount = 0;

            foreach (var cardData in allPhotocards)
            {
                int itemId = cardData.item_id;
                string cardName = cardData.item_name;
                bool isOwned = PhotocardHelper.HasPhotocard(itemId);
                bool isSelected = itemId == _pendingSelectedId;  // 선택 대기 ID로 표시

                if (isOwned) ownedCount++;

                // 스프라이트 로드 (동기적 시도)
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(cardData.prefab))
                {
                    sprite = ResourceManager.Instance.GetSprite(cardData.prefab);
                }

                var go = Instantiate(photocardItemPrefab, contentRoot);
                go.SetActive(true);
                _spawnedItems.Add(go);

                var item = go.GetComponent<PhotocardSlotItemUI>();
                if (item != null)
                {
                    item.Setup(this, itemId, sprite, cardName, isOwned, isSelected);
                    _items.Add(item);
                }
            }

            UpdateTitleText(ownedCount, allPhotocards.Count);

            // 레이아웃 강제 갱신
            if (contentRoot is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // 스프라이트가 없는 아이템들 비동기 로드
            _ = LoadMissingSpritesAsync(allPhotocards);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PhotocardSelectPopup] RebuildListAsync error: {ex.Message}");
        }
        finally
        {
            if (listCanvasGroup != null)
                listCanvasGroup.alpha = 1;

            NoteLoadingUI.Hide();
        }
    }

    private void UpdateTitleText(int owned, int total)
    {
        if (titleText != null)
        {
            string charName = PhotocardHelper.GetCharNameByCode(_charCode) ?? _charCode;
            titleText.text = $"{charName} 포토카드 ({owned}/{total})";
        }
    }

    private async UniTask LoadMissingSpritesAsync(List<ItemCSVData> photocards)
    {
        for (int i = 0; i < photocards.Count && i < _items.Count; i++)
        {
            var item = _items[i];
            var cardData = photocards[i];

            // 이미 스프라이트가 있으면 스킵
            if (!string.IsNullOrEmpty(cardData.prefab))
            {
                var existingSprite = ResourceManager.Instance.GetSprite(cardData.prefab);
                if (existingSprite != null) continue;
            }

            // Addressable에서 스프라이트 로드
            var sprite = await PhotocardHelper.LoadPhotocardSprite(cardData.item_id);
            if (sprite != null && item != null)
            {
                item.SetSprite(sprite);
            }
        }
    }

    public void OnClickItem(PhotocardSlotItemUI clickedItem)
    {
        if (!clickedItem.IsOwned) return;

        // 같은 카드 클릭시 선택 해제 (기본 카드로)
        if (clickedItem.ItemId == _pendingSelectedId)
        {
            // 기본 카드 ID 가져오기
            var allCards = PhotocardHelper.GetAllPhotocards(_charCode);
            _pendingSelectedId = allCards.Count > 0 ? allCards[0].item_id : 0;
        }
        else
        {
            // 새 카드 선택 (아직 저장 안함)
            _pendingSelectedId = clickedItem.ItemId;
        }

        // 선택 마크 업데이트 (시각적으로만)
        foreach (var item in _items)
        {
            item.SetSelected(item.ItemId == _pendingSelectedId);
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    /// <summary>
    /// 완료 버튼 클릭 - 선택 확정 및 저장
    /// </summary>
    private void OnConfirm()
    {
        if (Time.frameCount == _openFrame) return;

        // 변경사항이 있을 때만 저장
        if (_pendingSelectedId != _equippedItemId)
        {
            // 기본 카드인지 확인
            var allCards = PhotocardHelper.GetAllPhotocards(_charCode);
            int defaultCardId = allCards.Count > 0 ? allCards[0].item_id : 0;

            if (_pendingSelectedId == defaultCardId || _pendingSelectedId == 0)
            {
                // 기본 카드로 설정 (명시적 장착 해제)
                PhotocardHelper.UnequipPhotocard(_charCode);
                ToastUI.Show("기본 포토카드로 변경되었습니다.");
            }
            else
            {
                // 새 카드 장착
                PhotocardHelper.EquipPhotocard(_charCode, _pendingSelectedId);
                ToastUI.Show("포토카드가 변경되었습니다.");
            }

            _equippedItemId = _pendingSelectedId;
            OnPhotocardChanged?.Invoke();

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundName.SFX_UI_ChangeCloth);
        }

        // 팝업 닫기
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OnDimClicked -= OnDimClicked;
            WindowManager.Instance.HideDimManual();
        }

        _isOpen = false;
        gameObject.SetActive(false);
    }
}
