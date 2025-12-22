using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpSelectSlot : MonoBehaviour
{
    public TextMeshProUGUI selectName;
    public Image selectImage;
    public TextMeshProUGUI selectDesc;

    [HideInInspector]
    public SelectData selectData;

    public void ChooseSlot()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Skill_Select);

        var buffIds = new List<(int id, float value)>();
        if(selectData.effect_type1 != 0)
        {
            buffIds.Add((selectData.effect_type1, selectData.value1));
        }
        if (selectData.effect_type2 != 0)
        {
            buffIds.Add((selectData.effect_type2, selectData.value2));
        }

        GameObject[] towers = GameObject.FindGameObjectsWithTag(Tag.Tower);
        foreach (GameObject tower in towers)
        {
            foreach(var buff in buffIds)
            {
                EffectRegistry.Apply(tower, buff.id, buff.value, 99999);

                if (buff.id == 3005) // 체력 증가일때 
                {
                    CharacterFence.Instance.Init();
                }
            }
        }
        StageManager.Instance.RestoreTimeScale();
    }

    public void Init(SelectData data)
    {
        selectData = data;
        selectName.text = data.select_name;
        selectImage.sprite = ResourceManager.Instance.GetSprite(data.prefab);
        selectDesc.text = data.info;
    }
}