using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;               // 시너지 아이콘 (없으면 슬롯 프레임만 보이게)
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject activeMark;    // 발동 여부 표시 (예: 빛나는 테두리)

    public int SynergyId { get; private set; }
    private SynergyCSVData csv;
    private bool isActive;

    public System.Action<SynergyButton> onClick;

    // 🔹 "빈 슬롯"으로 만들기
    public void InitEmpty()
    {
        csv = null;
        SynergyId = 0;
        isActive = false;

        if (nameText != null)
            nameText.text = "";              // 이름 비움

        // 아이콘은 숨기되, 버튼 프레임(버튼의 자체 Image)은 안 건드림
        if (icon != null)
            icon.gameObject.SetActive(false);

        if (activeMark != null)
            activeMark.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.interactable = false;         // 빈 슬롯은 클릭 안 되게
    }

    public void Init(SynergyCSVData data, bool active)
    {
        csv = data;
        SynergyId = data.synergy_id;
        isActive = active;

        if (nameText != null)
            nameText.text = data.synergy_name;

        if (icon != null)
            icon.gameObject.SetActive(true);

        if (activeMark != null)
            activeMark.SetActive(active);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(this));
        button.interactable = true;
    }

    public SynergyCSVData GetData() => csv;
    public bool IsActive => isActive;

    public void SetActive(bool active)
    {
        isActive = active;

        if (activeMark != null)
            activeMark.SetActive(active);
    }
}
