using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragZoomPanManager : MonoBehaviour
{
    public static DragZoomPanManager Instance;

    [SerializeField] private RawImage lobbyRawImage;
    [SerializeField] private Camera lobbyHomeCamera;

    [SerializeField] private float panSpeed = 0.0015f; // 패닝 속도
    [SerializeField] private float zoomSpeed = 1f; // 줌 속도
    [SerializeField] private float minSize = 1.5f; // 최소 줌 크기
    private float maxSize = 7.94f; // 최대 줌 크기(바꾸지 말기)

    [SerializeField] private float edgeSize = 150f; // RawImage edge 영역 px
    [SerializeField] private float edgePanSpeed = 1f; // 에지 패닝 속도

    [SerializeField] private SpriteRenderer background; 
    [SerializeField] private BoxCollider2D innerBoundCollider;
    [SerializeField] private LayerMask dragTargetLayer;

    private bool isDraggingObject = false;
    private bool isPanning = false;
    private Transform dragTarget;
    private Vector2 lastPos;
    private Vector3 dragOffset; // 드래그 오프셋

    public Bounds BackgroundBounds => background.bounds;
    public Bounds InnerBounds => innerBoundCollider.bounds;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleTouchInput();
#endif
    }

    // Editor 입력 처리
    private void HandleEditorInput()
    {
        // UI가 클릭 중이면 월드 조작 금지
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // 줌 처리
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            ZoomAt(Input.mousePosition, scroll * zoomSpeed);
        }

        // 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 screen = Input.mousePosition;

            // RawImage 외부면 월드 조작 무시
            if (!RectTransformUtility.RectangleContainsScreenPoint(lobbyRawImage.rectTransform, screen))
            {
                isDraggingObject = false;
                isPanning = false;
                dragTarget = null;
                return;
            }

            // RawImage 내부 클릭 → 월드 좌표 변환
            Vector3 world;
            if (!TryGetWorldPositionFromRawImage(screen, out world))
                return;

            Collider2D hitCol = Physics2D.OverlapPoint(world, dragTargetLayer);
            // 드래그 가능한 오브젝트 클릭 시
            if (hitCol != null && hitCol.CompareTag(Tag.LobbyHomeObject))
            {
                isDraggingObject = true;
                dragTarget = hitCol.transform;

                dragOffset = dragTarget.position - world;
                // 캐릭터 드래그 할 때
                dragTarget.GetComponent<LobbyCharacterAI>()?.OnDragStart();
            }
            // 아니면 카메라 이동
            else
            {
                isPanning = true;
            }

            lastPos = screen;
        }

        // 클릭 중
        if (Input.GetMouseButton(0))
        {
            // RawImage 영역 밖에서 드래그해도 무시
            if (!RectTransformUtility.RectangleContainsScreenPoint(lobbyRawImage.rectTransform, Input.mousePosition))
                return;

            if (isDraggingObject)
            {
                DragObject(Input.mousePosition);
                // 가장자리 체크
                HandleDragEdgeScroll(Input.mousePosition);
            }
            else if (isPanning)
            {
                PanCamera(Input.mousePosition);
            }
        }

        // 클릭 종료
        if (Input.GetMouseButtonUp(0))
        {
            dragTarget?.GetComponent<LobbyCharacterAI>()?.OnDragEnd();

            isDraggingObject = false;
            isPanning = false;
            dragTarget = null;
        }
    }

    // 모바일 입력 처리
    private void HandleTouchInput()
    {
        // UI가 클릭 중이면 월드 조작 금지
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (IsTouchOverUI(t))
                return;
        }

        // 핀치 줌
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            Vector2 mid = (t1.position + t2.position) * 0.5f;

            Vector2 t1Prev = t1.position - t1.deltaPosition;
            Vector2 t2Prev = t2.position - t2.deltaPosition;

            float prevDist = Vector2.Distance(t1Prev, t2Prev);
            float currDist = Vector2.Distance(t1.position, t2.position);

            float diff = currDist - prevDist;

            ZoomAt(mid, diff * zoomSpeed * 0.01f);
            return;
        }

        // 단일 터치로 드래그 / 패닝
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            Vector2 screen = t.position;

            // RawImage 외부 터치 무시
            if (!RectTransformUtility.RectangleContainsScreenPoint(lobbyRawImage.rectTransform, screen))
                return;

            if (t.phase == TouchPhase.Began)
            {
                Vector3 world;
                if (!TryGetWorldPositionFromRawImage(screen, out world))
                    return;

                Collider2D hitCol = Physics2D.OverlapPoint(world, dragTargetLayer);
                if (hitCol != null && hitCol.CompareTag(Tag.LobbyHomeObject))
                {
                    isDraggingObject = true;
                    dragTarget = hitCol.transform;

                    dragOffset = dragTarget.position - world;
                    dragTarget.GetComponent<LobbyCharacterAI>()?.OnDragStart();
                }
                else
                {
                    isPanning = true;
                }

                lastPos = screen;
            }

            if (t.phase == TouchPhase.Moved)
            {
                if (isDraggingObject)
                {
                    DragObject(t.position);
                    // 가장자리 체크
                    HandleDragEdgeScroll(t.position);
                }
                else if (isPanning)
                {
                    PanCamera(t.position);
                }
            }

            if (t.phase == TouchPhase.Ended)
            {
                dragTarget?.GetComponent<LobbyCharacterAI>()?.OnDragEnd();

                isDraggingObject = false;
                isPanning = false;
                dragTarget = null;
            }
        }
    }

    // 오브젝트 드래그
    private void DragObject(Vector2 screenPos)
    {
        Vector3 worldPos;
        if (!TryGetWorldPositionFromRawImage(screenPos, out worldPos))
            return;

        Vector3 targetPos = worldPos + dragOffset;
        Bounds bounds = InnerBounds;

        // 드래그 대상 스프라이트 크기
        SpriteRenderer sr = dragTarget.GetComponent<SpriteRenderer>();
        Vector2 extents = sr != null ? sr.bounds.extents : Vector2.zero;

        targetPos.x = Mathf.Clamp(
            targetPos.x,
            bounds.min.x + extents.x,
            bounds.max.x - extents.x
        );

        targetPos.y = Mathf.Clamp(
            targetPos.y,
            bounds.min.y + extents.y,
            bounds.max.y - extents.y
        );

        dragTarget.position = targetPos;
    }

    // 카메라 이동
    private void PanCamera(Vector2 screenPos)
    {
        Vector2 delta = screenPos - lastPos;
        Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * lobbyHomeCamera.orthographicSize;

        lobbyHomeCamera.transform.position += move;

        ClampCamera();

        lastPos = screenPos;
    }

    // 카메라 줌인 줌아웃
    private void ZoomAt(Vector2 screenPos, float delta)
    {
        Vector3 worldBefore;
        if (!TryGetWorldPositionFromRawImage(screenPos, out worldBefore))
            return;

        lobbyHomeCamera.orthographicSize = Mathf.Clamp(
            lobbyHomeCamera.orthographicSize - delta,
            minSize, maxSize
        );

        Vector3 worldAfter;
        TryGetWorldPositionFromRawImage(screenPos, out worldAfter);

        lobbyHomeCamera.transform.position += (worldBefore - worldAfter);

        ClampCamera();

        // 줌 중에는 dragOffset 재계산
        if (isDraggingObject && dragTarget != null)
        {
            Vector3 world;
            if (TryGetWorldPositionFromRawImage(screenPos, out world))
            {
                dragOffset = dragTarget.position - world;
            }
        }
    }

    // 카메라가 배경 밖으로 안가게 보간
    private void ClampCamera()
    {
        float camHeight = lobbyHomeCamera.orthographicSize * 2f;
        float camWidth = camHeight * lobbyHomeCamera.aspect;

        float halfW = camWidth / 2f;
        float halfH = camHeight / 2f;

        Vector3 pos = lobbyHomeCamera.transform.position;

        pos.x = Mathf.Clamp(pos.x,
            background.bounds.min.x + halfW,
            background.bounds.max.x - halfW);

        pos.y = Mathf.Clamp(pos.y,
            background.bounds.min.y + halfH,
            background.bounds.max.y - halfH);

        lobbyHomeCamera.transform.position = pos;
    }

    // RawImage → World 좌표 변환
    private bool TryGetWorldPositionFromRawImage(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        RectTransform rt = lobbyRawImage.rectTransform;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
            return false;

        // RawImage 내부의 로컬 좌표로 변환
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out localPos);
        // RawImage 안에서의 UV 좌표(0~1) 얻음
        Rect rect = rt.rect;
        float uvX = (localPos.x - rect.x) / rect.width;
        float uvY = (localPos.y - rect.y) / rect.height;
        // UV → Camera pixel 좌표로 변환
        float camX = uvX * lobbyHomeCamera.pixelWidth;
        float camY = uvY * lobbyHomeCamera.pixelHeight;
        // 카메라 스크린 좌표 완성
        Vector3 camScreen = new Vector3(camX, camY, 0f);
        // 카메라 ScreenToWorldPoint 호출
        worldPos = lobbyHomeCamera.ScreenToWorldPoint(camScreen);
        worldPos.z = 0f;

        return true;
    }

    // 드래그 오브젝트 드래그 중이고, 가장자리로 가져갈시 패닝
    private void HandleDragEdgeScroll(Vector2 screenPos)
    {
        RectTransform rt = lobbyRawImage.rectTransform;

        // RawImage 내부가 아니면 패닝 X
        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
            return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out localPos);
        Rect rect = rt.rect;

        Vector3 camMove = Vector3.zero;

        // 왼쪽 에지
        if (localPos.x < rect.xMin + edgeSize)
            camMove.x -= edgePanSpeed * lobbyHomeCamera.orthographicSize * Time.deltaTime;

        // 오른쪽 에지
        if (localPos.x > rect.xMax - edgeSize)
            camMove.x += edgePanSpeed * lobbyHomeCamera.orthographicSize * Time.deltaTime;

        // 아래쪽 에지
        if (localPos.y < rect.yMin + edgeSize)
            camMove.y -= edgePanSpeed * lobbyHomeCamera.orthographicSize * Time.deltaTime;

        // 위쪽 에지
        if (localPos.y > rect.yMax - edgeSize)
            camMove.y += edgePanSpeed * lobbyHomeCamera.orthographicSize * Time.deltaTime;

        if (camMove != Vector3.zero)
        {
            lobbyHomeCamera.transform.position += camMove;
            ClampCamera();

            // 카메라 이동 후 드래그 오브젝트 위치 재보정
            if (isDraggingObject && dragTarget != null)
            {
                DragObject(screenPos);
            }
        }
    }

    // UI 터치중인지 확인
    private bool IsTouchOverUI(Touch touch)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touch.position;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }
}