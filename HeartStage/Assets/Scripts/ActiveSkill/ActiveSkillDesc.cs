using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveSkillDesc : MonoBehaviour
{
    public Image skillIconImage;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescText;

    public void ShowDesc(int skillId)
    {
        var skillData = DataTableManager.SkillTable.Get(skillId);
        skillIconImage.sprite = ResourceManager.Instance.GetSprite(skillData.icon_prefab);
        skillNameText.text = skillData.skill_name;
        skillDescText.text = skillData.GetFormattedInfo();
        gameObject.SetActive(true);
    }
}