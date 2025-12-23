using UnityEngine;

public class CharacterSpeechBubbleFollower : MonoBehaviour
{
    [SerializeField] private RectTransform friendCheerUp;
    [SerializeField] private RectTransform rewardSpeechBubble;
    [SerializeField] private RectTransform cheerEffect;

    [SerializeField] private Vector3 topOffset = new(0f, 1.6f, 0f);
    [SerializeField] private Vector3 leftOffset = new(-1f, 1.6f, 0f);
    [SerializeField] private Vector3 rightOffset = new(1f, 1.6f, 0f);

    public Camera lobbyHomeCamera;
    private Transform target;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        gameObject.SetActive(target != null);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        UpdateBubble(friendCheerUp, leftOffset);
        UpdateBubble(rewardSpeechBubble, rightOffset);
        UpdateBubble(cheerEffect, topOffset);
    }

    private void UpdateBubble(RectTransform bubble, Vector3 worldOffset)
    {
        // 월드 좌표
        Vector3 worldPos = target.position + worldOffset;
        // Viewport 좌표 (0~1)
        Vector3 viewportPos = lobbyHomeCamera.WorldToViewportPoint(worldPos);
        // RawImage 크기 기준 로컬 좌표로 변환
        RectTransform parentRect = bubble.parent as RectTransform;
        Vector2 size = parentRect.rect.size;

        Vector2 localPos = new Vector2(
            (viewportPos.x - 0.5f) * size.x,
            (viewportPos.y - 0.5f) * size.y
        );
        // 위치 적용
        bubble.localPosition = localPos;
    }
}