using UnityEngine;

public class CharacterSpeechBubbleFollower : MonoBehaviour
{
    [SerializeField] private RectTransform friendCheerUp;
    [SerializeField] private RectTransform rewardSpeechBubble;

    [SerializeField] private Vector3 leftOffset = new(-0.6f, 1.2f, 0f);
    [SerializeField] private Vector3 rightOffset = new(0.6f, 1.2f, 0f);

    public Camera lobbyHomeCamera;
    private Transform target;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

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
    }

    private void UpdateBubble(RectTransform bubble, Vector3 worldOffset)
    {
        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = lobbyHomeCamera.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out Vector2 localPos
        );

        bubble.anchoredPosition = localPos;
    }
}