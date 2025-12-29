using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // drag
    public bool dragOnSurfaces = true;
    private readonly Dictionary<int, GameObject> m_DraggingIcons = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, RectTransform> m_DraggingPlanes = new Dictionary<int, RectTransform>();

    // drop
    public Image containerImage;
    public Image receivingImage;
    //private Color normalColor;
    //public Color highlightColor = Color.yellow;
    public CharacterData characterData;

    [Header("빈 슬롯 기본 이미지")]
    [SerializeField] private Sprite emptySlotSprite;

    //드래그 슬롯 이벤트 추가
    public static System.Action OnAnySlotChanged;
    //부모추가
    private StageSetupWindow _window;
    public int slotIndex;

    private void Awake()
    {
        _window = GetComponentInParent<StageSetupWindow>();
    }

    private void NotifySlotChanged() => OnAnySlotChanged?.Invoke();

    // ---------- Drag (슬롯 자체도 드래그 가능하도록) ----------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DragMe.s_IsAnyDragActive && DragMe.s_ActivePointerId != eventData.pointerId)
            return;

        var canvas = FindInParents<Canvas>(gameObject);
        if (canvas == null)
            return;
        // 캐릭터 올려놓지 않으면 드래그 불가
        if (characterData == null)
            return;

        var icon = new GameObject("icon");
        m_DraggingIcons[eventData.pointerId] = icon;

        icon.transform.SetParent(canvas.transform, false);
        icon.transform.SetAsLastSibling();

        var image = icon.AddComponent<Image>();
        // 슬롯에 표시된 이미지 우선 사용
        var src = receivingImage != null ? receivingImage : GetComponent<Image>();
        if (src != null) image.sprite = src.sprite;
        // image.SetNativeSize();
        image.raycastTarget = false;

        var group = icon.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        if (dragOnSurfaces)
            m_DraggingPlanes[eventData.pointerId] = transform as RectTransform;
        else
            m_DraggingPlanes[eventData.pointerId] = canvas.transform as RectTransform;

        SetDraggedPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_DraggingIcons.TryGetValue(eventData.pointerId, out var icon) && icon != null)
        {
            SetDraggedPosition(eventData);
        }

    }

    private void SetDraggedPosition(PointerEventData eventData)
    {
        if (dragOnSurfaces && eventData.pointerEnter != null && eventData.pointerEnter.transform is RectTransform)
            m_DraggingPlanes[eventData.pointerId] = eventData.pointerEnter.transform as RectTransform;

        var rt = m_DraggingIcons[eventData.pointerId].GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                m_DraggingPlanes[eventData.pointerId],
                eventData.position,
                eventData.pressEventCamera,
                out var globalMousePos))
        {
            rt.position = globalMousePos;
            rt.rotation = m_DraggingPlanes[eventData.pointerId].rotation;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_DraggingIcons.TryGetValue(eventData.pointerId, out var icon) && icon != null)
            Destroy(icon);

        m_DraggingIcons.Remove(eventData.pointerId);  // <- null 넣지 말고 제거
        m_DraggingPlanes.Remove(eventData.pointerId);

        // 드래그 종료 시 패시브 미리보기 정리 (TrashZone 드롭 등 모든 케이스 커버)
        _window?.ClearPassivePreview();
    }

    public static T FindInParents<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        var comp = go.GetComponent<T>();
        if (comp != null) return comp;

        var t = go.transform.parent;
        while (t != null && comp == null)
        {
            comp = t.gameObject.GetComponent<T>();
            t = t.parent;
        }
        return comp;
    }

    // ---------- Drop ----------
    private void OnEnable()
    {
        //if (containerImage != null)
        //    normalColor = containerImage.color;
    }

    public void OnDrop(PointerEventData data)
    {
        if (!TryGetDropPayload(data, out var dropSprite, out var droppedCD))
            return;

        // DragMe(로비 목록)에서 새로 끌어오는 경우에만 제한 걸기
        bool targetEmpty = (characterData == null);
        bool fromDragMe = data.pointerDrag != null && data.pointerDrag.GetComponent<DragMe>() != null;

        if (fromDragMe && targetEmpty && _window != null && _window.IsDeployLimitReached())
            return;

        // (1) DragMe -> Slot 케이스
        if (fromDragMe)
        {
            // 드랍 들어오기 전에, 슬롯에 이미 들어있던 캐릭터 잠시 저장
            var prevCD = characterData;

            // 이전 캐릭터가 있고, 그것과 다른 캐릭터를 올리는 경우 → 먼저 풀어준다
            if (prevCD != null && prevCD != droppedCD)
            {
                ClearSlotAndUnlockSource(prevCD);
            }

            // 새 캐릭터를 이 슬롯에 배치
            if (receivingImage != null && dropSprite != null)
                receivingImage.sprite = dropSprite;

            characterData = droppedCD;

            // ★★★ 여기서 DragMe 잠그기 ★★★
            var src = data.pointerDrag.GetComponent<DragMe>();
            if (src != null)
            {
                DragSourceRegistry.Register(characterData, src);
                src.SetLocked(true);      // <- 이게 핵심
            }

            NotifySlotChanged();
            _window?.ClearPassivePreview();

            return;
        }

        // (2) Slot -> Slot 케이스 (그대로 사용)
        var sourceSlot = data.pointerDrag != null ? data.pointerDrag.GetComponent<DraggableSlot>() : null;
        if (sourceSlot != null)
        {
            HandleDropFromSlot(sourceSlot);
            NotifySlotChanged();
            if (characterData != null) Debug.Log($"{characterData.char_name}");
        }
    }


    public void OnPointerEnter(PointerEventData data)
    {
        // 드래그 중이 아니거나, 드롭 페이로드를 못 얻으면 리턴
        if (!TryGetDropPayload(data, out var sprite, out var cd))
            return;

        // 아직 진짜로 슬롯에 올린 건 아니고, "여기다 두면" 기준으로 미리보기
        if (_window != null && cd != null)
        {
            _window.ShowPassivePreview(slotIndex, cd);
        }
    }

    public void OnPointerExit(PointerEventData data)
    {
        // 슬롯에서 포인터가 나가면 미리보기 제거
        if (_window != null)
        {
            _window.ClearPassivePreview();
        }
    }

    /// <summary>
    /// DragMe에서 끌어오든, 다른 DraggableSlot에서 끌어오든 공통으로 Sprite/CharacterData를 얻는다.
    /// </summary>
    private bool TryGetDropPayload(PointerEventData data, out Sprite sprite, out CharacterData cd)
    {
        sprite = null;
        cd = null;

        var srcObj = data?.pointerDrag;
        if (srcObj == null) return false;

        // 1) DragMe에서 끌어온 경우
        var dragMe = srcObj.GetComponent<DragMe>();
        if (dragMe != null)
        {
            // 슬롯에 올라가 잠겨 있거나,
            // 세로 드래그가 아니라면(= 가로 스크롤 중이라면) 드롭/미리보기 무시
            if (dragMe.IsLocked || !dragMe.IsVerticalDrag)
                return false;

            var img = srcObj.GetComponent<Image>();
            if (img != null) sprite = img.sprite;
            cd = dragMe.characterData;
            return (sprite != null || cd != null);
        }

        // 2) 다른 DraggableSlot에서 끌어온 경우 (슬롯 → 슬롯)
        var slot = srcObj.GetComponent<DraggableSlot>();
        if (slot != null)
        {
            var img = slot.receivingImage != null ? slot.receivingImage : slot.GetComponent<Image>();
            if (img != null) sprite = img.sprite;
            cd = slot.characterData;
            return (sprite != null || cd != null);
        }

        return false;
    }

    public void ClearSlotAndUnlockSource(CharacterData leaving)
    {
        if (leaving == null) return;

        if (receivingImage) receivingImage.sprite = emptySlotSprite;
        if (characterData == leaving) characterData = null;

        var src = DragSourceRegistry.GetSource(leaving);
        if (src != null)
        {
            // 예전 raycastTarget = true 대신
            src.SetLocked(false);   // 다시 흰색 + 드래그 가능
        }

        NotifySlotChanged();
    }

    private Sprite GetSlotSprite(DraggableSlot slot)
    {
        var img = slot.receivingImage != null ? slot.receivingImage : slot.GetComponent<Image>();
        return img != null ? img.sprite : null;
    }

    // 슬롯의 표시 스프라이트 설정 (null이면 빈 슬롯 이미지 적용)
    private void SetSlotSprite(DraggableSlot slot, Sprite s)
    {
        var img = slot.receivingImage != null ? slot.receivingImage : slot.GetComponent<Image>();
        if (img != null) img.sprite = s != null ? s : slot.emptySlotSprite;
    }

    // Slot -> Slot 드롭 처리 (스왑/이동)
    private void HandleDropFromSlot(DraggableSlot source)
    {
        if (source == null || source == this) return;        // 같은 슬롯이면 무시
        if (source.characterData == null) return;            // 소스 비었으면 무시

        var srcCD = source.characterData;
        var srcSprite = GetSlotSprite(source);

        if (this.characterData == null)
        {
            // ------- 이동 (타깃 빈칸) -------
            SetSlotSprite(this, srcSprite);
            this.characterData = srcCD;

            // 원 슬롯 비우기 (DragMe 해제는 삭제일 때만, 이동은 해제하지 않음)
            SetSlotSprite(source, null);
            source.characterData = null;
        }
        else
        {
            // ------- 스왑 (서로 데이터 있음) -------
            var dstCD = this.characterData;
            var dstSprite = GetSlotSprite(this);

            // 소스 -> 타깃
            SetSlotSprite(this, srcSprite);
            this.characterData = srcCD;

            // 타깃(원래 것) -> 소스
            SetSlotSprite(source, dstSprite);
            source.characterData = dstCD;
        }
    }
}
