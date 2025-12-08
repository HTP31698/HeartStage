using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

public class StageWindow : GenericWindow
{
    [Header("UI References")]
    public RectTransform contentParent;
    public GameObject stagePrefab; // 피벗 (0.5,0), 앵커 (0.5,0)    
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private StageInfoWindow stageInfoUI;

    [Header("Button")]
    //[SerializeField] private Button closeButton;
    [SerializeField] private Button stageInfoButton;

    private float verticalSpacing = 600f; // 세로 간격
    private float horizontalOffset = 350f; // 좌우 번갈이 거리
    private float verticalPadding = 100f; // 화면 상단 패딩

    [Header("Field")]
    private StageTable stageTable;

    private void Awake()
    {
        isOverlayWindow = true; // 오버레이 창으로 설정
    }

    // 자식 오브젝트 삭제
    [ContextMenu("DeleteChildren")]
    public void DeleteChildren()
    {
        for (int i = contentParent.childCount - 1; i >= 1; i--)
        {            
            DestroyImmediate(contentParent.GetChild(i).gameObject);
        }
    }

    // 스테이지 이미지 간격에 맞게 생성
    [ContextMenu("GenerateStages()")]
    public void GenerateStages()
    {
        DeleteChildren();

        if (stageTable == null)
        {
            stageTable = DataTableManager.StageTable;
        }

        var allStages = stageTable.GetOrderedStages();

        allStages.Sort((x, y) =>
        {
            int result = x.stage_step1.CompareTo(y.stage_step1);
            if (result == 0)
            {
                result = x.stage_step2.CompareTo(y.stage_step2);
            }
            return result;
        });

        int index = 0;

        for (int i = 0; i < allStages.Count; i++)
        {
            var stageData = allStages[i];
            var sb = new StringBuilder();
            sb.Clear();
            sb.Append($"{stageData.stage_step1} - {stageData.stage_step2}");

            GameObject stageObj = Instantiate(stagePrefab, contentParent);

            RectTransform rect = stageObj.GetComponent<RectTransform>();

            float y = index * verticalSpacing + verticalPadding;
            float x = (index % 2 == 0) ? -horizontalOffset : horizontalOffset;

            rect.anchoredPosition = new Vector2(x, y);

            var text = stageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = sb.ToString();

            var button = stageObj.GetComponent<Button>();
            if (button != null)
            {
                // 지역 변수에 복사하여 각각의 값을 캡처
                var capturedStageData = stageData; // 클로저 문제 해결
                button.onClick.AddListener(() => OnStageInfoButtonClicked(capturedStageData));
            }

            index++;
        }

        // Content의 높이 자동 조정
        float contentHeight = (allStages.Count - 1) * verticalSpacing + 500f;
        Vector2 size = contentParent.sizeDelta;
        size.y = contentHeight;
        contentParent.sizeDelta = size;
    }

    private void OnStageInfoButtonClicked(StageCSVData stageData)
    {
        if (stageInfoUI != null)
        {
            stageInfoUI.SetStageData(stageData);
        }

        windowManager.OpenOverlay(WindowType.StageInfo);
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    public override void Open()
    {
        base.Open();
        GenerateStages();
    }
    public override void Close()
    {
        base.Close();
    }
}
