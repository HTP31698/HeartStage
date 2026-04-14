using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public int item_id;
    public string item_name;
    public int item_type;
    public int item_use;
    public bool item_inv;
    public int item_dup;
    public string item_desc;
    public string prefab;
    public string prefab_frame;
    public string char_code;

    // CSV → ScriptableObject 업데이트
    public void UpdateData(ItemCSVData data)
    {
        item_id = data.item_id;
        item_name = data.item_name;
        item_type = data.item_type;
        item_use = data.item_use;
        item_inv = data.item_inv;
        item_dup = data.item_dup;
        item_desc = data.item_desc;
        prefab = data.prefab;
        prefab_frame = data.prefab_frame;
        char_code = data.char_code;
    }

    // ScriptableObject → CSV 데이터 변환
    public ItemCSVData ToCSVData()
    {
        return new ItemCSVData
        {
            item_id = item_id,
            item_name = item_name,
            item_type = item_type,
            item_use = item_use,
            item_inv = item_inv,
            item_dup = item_dup,
            item_desc = item_desc,
            prefab = prefab,
            prefab_frame = prefab_frame,
            char_code = char_code
        };
    }
}
