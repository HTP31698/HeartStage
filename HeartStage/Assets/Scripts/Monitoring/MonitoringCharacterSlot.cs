using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MonitoringCharacterSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image characterImage;
    [SerializeField] private Image slotBackGroundImage; 

    private CharacterData currentCharacterData;
    private int slotIndex;
    private MonitoringCharacterSelectUI parentUI;

    // 드래그 관련 변수들
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GameObject dragIcon;

    private bool isSlotEnabled = true;
    private Color originalSlotColor;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rectTransform = GetComponent<RectTransform>();

        if(slotBackGroundImage == null)
        {
             slotBackGroundImage = GetComponent<Image>();
        }

        if(slotBackGroundImage != null)
        {
           originalSlotColor = slotBackGroundImage.color;
        }
    }

    public void Init(int index, MonitoringCharacterSelectUI parent)
    {
        slotIndex = index;
        parentUI = parent;
        UpdateVisualState();
    }

    public bool IsEmpty() => currentCharacterData == null;
    public CharacterData GetCharacterData() => currentCharacterData;

    public void SetSlotEnabled(bool enabled)
    {
        isSlotEnabled = enabled;
        UpdateSlotColor();
        UpdateVisualState();
    }

    private void UpdateSlotColor()
    {
        if (slotBackGroundImage == null)
        {
            return;
        }

        if (isSlotEnabled)
        {
            slotBackGroundImage.color = originalSlotColor;
        }
        else
        {
            slotBackGroundImage.color = Color.red;
        }
    }

    public bool TrySetCharacter(CharacterData characterData)
    {
        if (characterData == null) return false;

        if(!isSlotEnabled)
        {
            return false; // 슬롯이 비활성화된 경우 캐릭터를 설정하지 않음
        }

        UnlockPreviousCharacter();
        currentCharacterData = characterData;
        UpdateCharacterImage();
        UpdateVisualState();

        return true;
    }

    public void ClearSlot()
    {
        UnlockPreviousCharacter();
        currentCharacterData = null;
        UpdateCharacterImage();
        UpdateVisualState();
    }

    private void UnlockPreviousCharacter()
    {
        if (currentCharacterData != null && parentUI != null)
        {
            parentUI.SetCharacterLocked(currentCharacterData, false);
        }
    }

    private void UpdateCharacterImage()
    {
        LoadPhotocardAsync(currentCharacterData).Forget();
    }

    private async UniTaskVoid LoadPhotocardAsync(CharacterData characterData)
    {
        if (characterData == null)
        {
            characterImage.sprite = null;
            return;
        }

        string charCode = PhotocardHelper.ExtractCharCode(characterData.char_id);
        // 스테이지 배치 UI에서는 Frame 버전 사용
        var sprite = await PhotocardHelper.LoadDisplaySpriteWithFrame(charCode);

        if (sprite != null)
        {
            characterImage.sprite = sprite;
        }
        else
        {
            // fallback: 기존 방식
            characterImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
        }
    }

    private void UpdateVisualState()
    {
        bool isEmpty = IsEmpty();
        float imageAlpha = isEmpty ? 0.3f : 1.0f;
        float slotAlpha = isEmpty ? 0.7f : 1.0f;

        // 캐릭터 이미지 투명도
        if (characterImage != null)
        {
            Color color = characterImage.color;
            color.a = imageAlpha;
            characterImage.color = color;
        }

        // 슬롯 자체 투명도
        if (canvasGroup != null)
        {
            canvasGroup.alpha = slotAlpha;
        }
    }

    #region 드래그 앤 드롭 구현

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentCharacterData == null || canvas == null) return;

        dragIcon = MonitoringDragHelper.CreateDragIcon(canvas, characterImage, rectTransform, eventData, "SlotDragIcon");

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        MonitoringDragHelper.UpdateDragIconPosition(dragIcon, canvas, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MonitoringDragHelper.DestroyDragIcon(ref dragIcon);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
        }

        CheckDropRemoval(eventData);
    }

    private void CheckDropRemoval(PointerEventData eventData)
    {
        bool droppedOnSlot = TryDropOnAnotherSlot(eventData);

        // 다른 슬롯에 드롭되지 않았으면 캐릭터 제거
        if (!droppedOnSlot && parentUI != null && currentCharacterData != null)
        {
            parentUI.RemoveCharacterFromSlot(currentCharacterData);
        }
    }

    private bool TryDropOnAnotherSlot(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null) return false;

        var targetSlot = eventData.pointerEnter.GetComponent<MonitoringCharacterSlot>();
        if (targetSlot == null || targetSlot == this) return false;

        if (targetSlot.IsEmpty() && parentUI != null)
        {
            var tempCharacter = currentCharacterData;
            ClearSlot();
            parentUI.TryPlaceCharacter(tempCharacter, targetSlot.slotIndex);
        }

        return true;
    }

    #endregion

    #region 드롭 처리 (캐릭터 목록에서 슬롯으로)

    public void OnDrop(PointerEventData eventData)
    {
        // 캐릭터 목록에서 드래그해온 경우
        if (TryHandleCharacterPrefabDrop(eventData)) return;

        // 다른 슬롯에서 드래그해온 경우
        TryHandleSlotDrop(eventData);
    }

    private bool TryHandleCharacterPrefabDrop(PointerEventData eventData)
    {
        var dragPrefab = eventData.pointerDrag?.GetComponent<MonitoringCharacterPrefab>();
        if (dragPrefab != null && dragPrefab.GetCharacterData() != null && !dragPrefab.IsLocked())
        {
            parentUI?.TryPlaceCharacter(dragPrefab.GetCharacterData(), slotIndex);
            return true;
        }
        return false;
    }

    private void TryHandleSlotDrop(PointerEventData eventData)
    {
        var dragSlot = eventData.pointerDrag?.GetComponent<MonitoringCharacterSlot>();
        if (dragSlot != null && dragSlot != this && dragSlot.GetCharacterData() != null)
        {
            if (parentUI != null && IsEmpty())
            {
                var droppedCharacter = dragSlot.GetCharacterData();
                dragSlot.ClearSlot();
                parentUI.TryPlaceCharacter(droppedCharacter, slotIndex);
            }
        }
    }

    #endregion
}