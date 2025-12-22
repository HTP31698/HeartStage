using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 캐릭터 프리팹에 붙는 의상 관리 컴포넌트.
/// SpriteRenderer 참조를 갖고, 의상 변경 시 스프라이트를 교체.
/// </summary>
public class CostumeController : MonoBehaviour
{
    [Header("캐릭터 정보")]
    [SerializeField] private string characterName;

    [Header("상의 SpriteRenderers (Top_1 ~ Top_5)")]
    [SerializeField] private SpriteRenderer[] topRenderers = new SpriteRenderer[5];

    [Header("하의 SpriteRenderers (Pants_1 ~ Pants_5)")]
    [SerializeField] private SpriteRenderer[] pantsRenderers = new SpriteRenderer[5];

    [Header("신발 SpriteRenderers (Shoes_1, Shoes_2)")]
    [SerializeField] private SpriteRenderer[] shoesRenderers = new SpriteRenderer[2];

    /// <summary>
    /// 현재 장착된 의상 정보
    /// </summary>
    public EquippedCostume CurrentCostume { get; private set; }

    public string CharacterName => characterName;

    /// <summary>
    /// 캐릭터 생성 시 호출. SaveData에서 의상 정보를 읽어 적용.
    /// </summary>
    public void Initialize(string charName)
    {
        characterName = charName;
        LoadEquippedCostume();
    }

    /// <summary>
    /// SaveData에서 장착 의상 정보 로드 및 적용
    /// </summary>
    public void LoadEquippedCostume()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        if (saveData.equippedCostumeByChar.TryGetValue(characterName, out var costume))
        {
            CurrentCostume = costume;
        }
        else
        {
            CurrentCostume = new EquippedCostume();
        }

        ApplyAllCostumes();
    }

    /// <summary>
    /// 모든 의상 적용 (Top, Pants, Shoes)
    /// </summary>
    public async void ApplyAllCostumes()
    {
        if (CurrentCostume.topItemId > 0)
            await CostumeHelper.ApplyCostume(this, CostumeType.Top, CurrentCostume.topItemId);

        if (CurrentCostume.pantsItemId > 0)
            await CostumeHelper.ApplyCostume(this, CostumeType.Pants, CurrentCostume.pantsItemId);

        if (CurrentCostume.shoesItemId > 0)
            await CostumeHelper.ApplyCostume(this, CostumeType.Shoes, CurrentCostume.shoesItemId);
    }

    /// <summary>
    /// 특정 타입의 의상 스프라이트 설정
    /// </summary>
    public void SetSprites(CostumeType type, Sprite[] sprites)
    {
        SpriteRenderer[] renderers = type switch
        {
            CostumeType.Top => topRenderers,
            CostumeType.Pants => pantsRenderers,
            CostumeType.Shoes => shoesRenderers,
            _ => null
        };

        if (renderers == null) return;

        int count = Mathf.Min(sprites.Length, renderers.Length);
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null && sprites[i] != null)
            {
                renderers[i].sprite = sprites[i];
            }
        }
    }

    /// <summary>
    /// 의상 장착 (SaveData 저장 + 스프라이트 적용)
    /// </summary>
    public async void EquipCostume(int itemId)
    {
        var type = CostumeItemID.GetCostumeType(itemId);

        // CurrentCostume 업데이트
        switch (type)
        {
            case CostumeType.Top:
                CurrentCostume.topItemId = itemId;
                break;
            case CostumeType.Pants:
                CurrentCostume.pantsItemId = itemId;
                break;
            case CostumeType.Shoes:
                CurrentCostume.shoesItemId = itemId;
                break;
        }

        // SaveData 업데이트
        var saveData = SaveLoadManager.Data;
        if (saveData != null)
        {
            saveData.equippedCostumeByChar[characterName] = CurrentCostume;
            SaveLoadManager.SaveToServer().Forget();
        }

        // 스프라이트 적용
        await CostumeHelper.ApplyCostume(this, type, itemId);
    }

    /// <summary>
    /// 의상 해제 (기본 의상으로)
    /// </summary>
    public void UnequipCostume(CostumeType type)
    {
        switch (type)
        {
            case CostumeType.Top:
                CurrentCostume.topItemId = 0;
                break;
            case CostumeType.Pants:
                CurrentCostume.pantsItemId = 0;
                break;
            case CostumeType.Shoes:
                CurrentCostume.shoesItemId = 0;
                break;
        }

        // SaveData 업데이트
        var saveData = SaveLoadManager.Data;
        if (saveData != null)
        {
            saveData.equippedCostumeByChar[characterName] = CurrentCostume;
            SaveLoadManager.SaveToServer().Forget();
        }

        // 기본 스프라이트로 복원은 별도 처리 필요
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 자동으로 SpriteRenderer 참조 찾기
    /// </summary>
    [ContextMenu("Auto Find Renderers")]
    private void AutoFindRenderers()
    {
        // Top (Top_1 ~ Top_5)
        for (int i = 0; i < 5; i++)
        {
            var tr = transform.Find($"Top_{i + 1}");
            if (tr != null)
                topRenderers[i] = tr.GetComponent<SpriteRenderer>();
        }

        // Pants (Pants_1 ~ Pants_5)
        for (int i = 0; i < 5; i++)
        {
            var tr = transform.Find($"Pants_{i + 1}");
            if (tr != null)
                pantsRenderers[i] = tr.GetComponent<SpriteRenderer>();
        }

        // Shoes (Shoes_1, Shoes_2)
        for (int i = 0; i < 2; i++)
        {
            var tr = transform.Find($"Shoes_{i + 1}");
            if (tr != null)
                shoesRenderers[i] = tr.GetComponent<SpriteRenderer>();
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CostumeController] Auto find complete for {gameObject.name}");
    }
#endif
}
