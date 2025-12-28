using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DragMe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool dragOnSurfaces = true;
    public CharacterData characterData;

    [SerializeField] private float directionThreshold = 10f;


    // ==== 전역 드래그 상태 (멀티 드래그 방지용) ====
    public static bool s_IsAnyDragActive = false;
    public static int s_ActivePointerId = -1;

    // ==== 상태 플래그 ====
    // 이번 드래그가 “세로 드래그”였는지 여부 (DraggableSlot/CharacterInfoTab에서 사용)
    public bool IsVerticalDrag { get; private set; }
    public bool WasDragging { get; private set; }      // 이번 드래그에서 움직임 있었는지
    public bool DragJustEnded { get; private set; }    // "드래그 끝난 다음 프레임" 클릭 무효 처리용

    // 슬롯에 올라간 상태(= 잠금)
    public bool IsLocked { get; private set; }

    // ==== 색상 ====
    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    // 124 / 124 / 124 / 255
    [SerializeField] private Color lockedColor = new Color32(124, 124, 124, 255);
    [SerializeField] private Color draggingColor = new Color32(124, 124, 124, 255);

    private Image _image;
    private bool _isDragging;

    // ==== 드래그 관련 구조체들 ====
    private readonly Dictionary<int, GameObject> m_DraggingIcons = new();
    private readonly Dictionary<int, RectTransform> m_DraggingPlanes = new();

    private enum DragDirection { Undecided, Horizontal, Vertical }
    private readonly Dictionary<int, DragDirection> m_Directions = new();
    private readonly Dictionary<int, Vector2> m_StartPointerPos = new();
    private readonly Dictionary<int, bool> m_ScrollDragging = new();

    private ScrollRect parentScrollRect;

    void Awake()
    {
        parentScrollRect = FindInParents<ScrollRect>(gameObject);
        _image = GetComponent<Image>();
        ApplyColor();

        if (characterData != null)
            DragSourceRegistry.Register(characterData, this);
    }

    private void OnDisable()
    {
        s_IsAnyDragActive = false;
        s_ActivePointerId = -1;
    }

    void OnDestroy()
    {
        if (characterData != null)
            DragSourceRegistry.Unregister(characterData, this);
    }

    public void RebindCharacter(CharacterData cd)
    {
        if (characterData == cd)
            return;

        if (characterData != null)
            DragSourceRegistry.Unregister(characterData, this);

        characterData = cd;

        if (characterData != null)
            DragSourceRegistry.Register(characterData, this);
    }

    // 잠금 상태 변경 (슬롯에서 호출)
    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_image == null) _image = GetComponent<Image>();
        if (_image == null) return;

        Color target = normalColor;

        if (_isDragging)
            target = draggingColor;    // 드래그 중
        else if (IsLocked)
            target = lockedColor;     // 슬롯에 올라간 상태

        _image.color = target;
        // 인포탭 클릭을 위해 항상 켜둠
        _image.raycastTarget = true;
    }

    // ================= DRAG =================

    public void OnBeginDrag(PointerEventData eventData)
    {
        // IsLocked 체크는 OnDrag에서 세로 드래그 확정 시에만 수행  
        // 가로 드래그(슬라이드)는 IsLocked 상태에서도 허용

        if (s_IsAnyDragActive && s_ActivePointerId != eventData.pointerId)
            return;
        // 여기서부터는 "처음 시작된" 드래그만 통과
        s_IsAnyDragActive = true;
        s_ActivePointerId = eventData.pointerId;

        int id = eventData.pointerId;

        WasDragging = false;
        DragJustEnded = false;

        _isDragging = false;
        IsVerticalDrag = false;

        m_StartPointerPos[id] = eventData.position;
        m_Directions[id] = DragDirection.Undecided;
        m_ScrollDragging[id] = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        int id = eventData.pointerId;

        if (!m_Directions.TryGetValue(id, out var dir))
            return;

        // 방향 아직 결정 안 된 상태면 먼저 판정
        if (dir == DragDirection.Undecided)
        {
            var startPos = m_StartPointerPos[id];
            var delta = eventData.position - startPos;

            if (delta.sqrMagnitude < directionThreshold * directionThreshold)
            {
                // 살짝이라도 움직이면 드래그로 간주해서 클릭 방지
                WasDragging = true;
                return;
            }

            WasDragging = true;

            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            {
                // ===== 세로 드래그 확정 =====
                m_Directions[id] = DragDirection.Vertical;
                IsVerticalDrag = true;

                // 슬롯에 올라간 상태면 세로 드래그(캐릭터 이동)는 금지
                if (IsLocked)
                    return;

                // 여기서만 드래그 색 적용
                _isDragging = true;
                ApplyColor();

                StartVerticalDrag(eventData);
            }
            else
            {
                // ===== 가로 드래그 확정 → ScrollRect에게 넘김 =====
                m_Directions[id] = DragDirection.Horizontal;

                if (parentScrollRect != null)
                {
                    parentScrollRect.OnBeginDrag(eventData);
                    parentScrollRect.OnDrag(eventData);
                    m_ScrollDragging[id] = true;
                }
                return;
            }
        }
        else if (dir == DragDirection.Vertical)
        {
            // 세로 드래그 중 → 아이콘 따라다니기
            if (m_DraggingIcons.TryGetValue(id, out var icon) && icon != null)
                SetDraggedPosition(eventData);

            // 슬롯 위가 아니면 패시브 미리보기 정리
            bool overSlot = eventData.pointerEnter != null &&
                            eventData.pointerEnter.GetComponent<DraggableSlot>() != null;
            if (!overSlot)
            {
                var stageWindow = FindInParents<StageSetupWindow>(gameObject);
                stageWindow?.ClearPassivePreview();
            }
        }
        else if (dir == DragDirection.Horizontal)
        {
            // 가로 드래그 중 → ScrollRect 스크롤
            if (parentScrollRect != null &&
                m_ScrollDragging.TryGetValue(id, out bool started) &&
                started)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
    }

    private void StartVerticalDrag(PointerEventData eventData)
    {
        var canvas = FindInParents<Canvas>(gameObject);
        if (canvas == null) return;

        int id = eventData.pointerId;

        var icon = new GameObject("icon");
        m_DraggingIcons[id] = icon;

        icon.transform.SetParent(canvas.transform, false);
        icon.transform.SetAsLastSibling();

        var srcImage = GetComponent<Image>();

        var image = icon.AddComponent<Image>();
        image.sprite = srcImage.sprite;
        image.raycastTarget = false;

        var group = icon.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        if (dragOnSurfaces)
            m_DraggingPlanes[id] = transform as RectTransform;
        else
            m_DraggingPlanes[id] = canvas.transform as RectTransform;

        SetDraggedPosition(eventData);
    }

    private void SetDraggedPosition(PointerEventData eventData)
    {
        int id = eventData.pointerId;

        if (!m_DraggingIcons.TryGetValue(id, out var icon) || icon == null)
            return;

        if (dragOnSurfaces &&
            eventData.pointerEnter != null &&
            eventData.pointerEnter.transform is RectTransform)
        {
            m_DraggingPlanes[id] = eventData.pointerEnter.transform as RectTransform;
        }

        if (!m_DraggingPlanes.TryGetValue(id, out var plane) || plane == null)
            return;

        var rt = icon.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                plane,
                eventData.position,
                eventData.pressEventCamera,
                out var globalMousePos))
        {
            rt.position = globalMousePos;
            rt.rotation = plane.rotation;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        int id = eventData.pointerId;

        // 세로 드래그였으면 아이콘 정리
        if (m_DraggingIcons.TryGetValue(id, out var icon) && icon != null)
            Destroy(icon);

        // 가로 드래그였으면 ScrollRect에 종료 알림
        if (m_Directions.TryGetValue(id, out var dir) &&
            dir == DragDirection.Horizontal &&
            parentScrollRect != null &&
            m_ScrollDragging.TryGetValue(id, out bool started) &&
            started)
        {
            parentScrollRect.OnEndDrag(eventData);
        }

        m_DraggingIcons.Remove(id);
        m_DraggingPlanes.Remove(id);
        m_StartPointerPos.Remove(id);
        m_Directions.Remove(id);
        m_ScrollDragging.Remove(id);

        _isDragging = false;
        ApplyColor();

        IsVerticalDrag = false;
        SetDragJustEndedFlag().Forget();

        // 드래그 종료 시 패시브 미리보기 정리
        var stageWindow = FindInParents<StageSetupWindow>(gameObject);
        stageWindow?.ClearPassivePreview();

        if (s_ActivePointerId == eventData.pointerId)
        {
            s_IsAnyDragActive = false;
            s_ActivePointerId = -1;
        }
    }

    // =============== UTIL ===============

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

    private async UniTaskVoid SetDragJustEndedFlag()
    {
        DragJustEnded = true;
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); // 1프레임 동안만 TAP 금지
        DragJustEnded = false;
    }
}
