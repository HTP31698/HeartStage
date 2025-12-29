using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    private Canvas _canvas;

    [Header("루트 패널")]
    [SerializeField] private GameObject root;     // LoadingPanel

    [Header("메인 로딩 이미지")]
    [SerializeField] private Image loadingArtImage;
    [SerializeField] private Sprite[] loadingSprites; // 랜덤/순환용 이미지들

    [Header("진행도 슬라이더")]
    [SerializeField] private Slider progressSlider;

    [Header("퍼센트 텍스트 (슬라이더 안/위)")]
    [SerializeField] private TextMeshProUGUI percentText;

    [Header("음표 로딩 인디케이터 (선택)")]
    [SerializeField] private LoadingIndicator loadingIndicator;

    // 현재 팁 비활성
    //[Header("선택: 로딩 팁 텍스트")]
    //[SerializeField] private TextMeshProUGUI tipText;
    //[TextArea]
    //[SerializeField] private string[] tips;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 1000; // 메인 Canvas보다 앞

        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        if (progressSlider != null)
            progressSlider.value = 0f;

        if (percentText != null)
            percentText.text = "0%";

        // 음표 애니메이션 시작
        if (loadingIndicator != null)
            loadingIndicator.gameObject.SetActive(true);

        SetupRandomImage();

        //현재 팁 비활성
        //SetupRandomTip();
    }

    public void Hide()
    {
        // 음표 애니메이션 정지
        if (loadingIndicator != null)
            loadingIndicator.gameObject.SetActive(false);

        if (root != null)
            root.SetActive(false);
        root.transform.SetAsLastSibling();
    }

    public void SetProgress(float value01)
    {
        float v = Mathf.Clamp01(value01);

        if (progressSlider != null)
            progressSlider.value = v;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(v * 100f)}%";
    }

    private void SetupRandomImage()
    {
        if (loadingArtImage == null || loadingSprites == null || loadingSprites.Length == 0)
            return;

        int idx = Random.Range(0, loadingSprites.Length);
        loadingArtImage.sprite = loadingSprites[idx];
    }

    //현재 팁 비활성
    //private void SetupRandomTip()
    //{
    //    if (tipText == null || tips == null || tips.Length == 0)
    //        return;

    //    int idx = Random.Range(0, tips.Length);
    //    tipText.text = tips[idx];
    //}
}
