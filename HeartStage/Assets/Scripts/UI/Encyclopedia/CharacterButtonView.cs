using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonView : MonoBehaviour
{
    [Header("이 버튼이 가리키는 캐릭터 ID")]
    public int charId;

    [Header("목록 썸네일")]
    public Image iconImage;

    [SerializeField] private Button button;

    private int _lastId = -1;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    private void Start()
    {
        if (charId != 0 && charId != _lastId)
            SetButton(charId);
    }

    public void SetButton(int characterId)
    {
        charId = characterId;
        _lastId = characterId;

        if (iconImage == null)
        {
            Debug.LogWarning("[CharacterButtonView] iconImage가 비어있음");
            return;
        }

        var data = DataTableManager.CharacterTable.Get(charId);
        if (data == null)
        {
            Debug.LogWarning($"[CharacterButtonView] 데이터 로드 실패: charId={charId}");
            return;
        }

        // 포토카드 교체 반영: 장착된 포토카드가 있으면 해당 이미지, 없으면 기본 카드
        LoadPhotocardAsync(data).Forget();
    }

    private async UniTaskVoid LoadPhotocardAsync(CharacterCSVData data)
    {
        string charCode = PhotocardHelper.ExtractCharCode(data.char_id);
        var sprite = await PhotocardHelper.LoadDisplaySprite(charCode);

        if (sprite != null)
        {
            iconImage.sprite = sprite;
        }
        else
        {
            // fallback: 기존 방식
            iconImage.sprite = ResourceManager.Instance.GetSprite(data.card_imageName);
        }
    }

    // ⭐ 잠김(회색) / 해금(정상)
    public void SetLocked(bool locked)
    {
        if (iconImage != null)
        {
            iconImage.color = locked
                ? new Color(0.35f, 0.35f, 0.35f, 1f)
                : Color.white;
        }

        //if (button != null)
        //    button.interactable = !locked; // 잠긴애 클릭 막기
    }
}

