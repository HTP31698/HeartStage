using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendCheerRewardUI : MonoBehaviour
{
    public Image rewardItemImage;
    public TextMeshProUGUI amountText;

    public float moveDistance = 80f;
    public float duration = 1.2f;

    private RectTransform rectTransform;
    private Vector2 startPos;
    private CanvasGroup canvasGroup;

    private float elapsed;
    private bool isPlaying;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        startPos = rectTransform.anchoredPosition;
        gameObject.SetActive(false);
    }

    public void Init(int rewardItemId, int amount)
    {
        rewardItemImage.sprite =            ResourceManager.Instance.GetSprite(                DataTableManager.ItemTable.Get(rewardItemId).prefab);
        amountText.text = $"x{amount} Get";
        Play();
    }

    private void Play()
    {
        elapsed = 0f;
        isPlaying = true;

        rectTransform.anchoredPosition = startPos;
        canvasGroup.alpha = 1f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        rectTransform.anchoredPosition =
            Vector2.Lerp(startPos, startPos + Vector2.up * moveDistance, t);

        canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

        if (t >= 1f)
        {
            isPlaying = false;
            gameObject.SetActive(false);
        }
    }
}