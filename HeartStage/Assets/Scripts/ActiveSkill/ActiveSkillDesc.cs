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
        var texture = ResourceManager.Instance.Get<Texture2D>(skillData.icon_prefab);
        skillIconImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        skillNameText.text = skillData.skill_name;
        skillDescText.text = skillData.GetFormattedInfo();
    }
}