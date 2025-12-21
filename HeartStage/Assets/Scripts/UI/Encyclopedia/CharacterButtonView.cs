using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonView : MonoBehaviour
{
    [Header("이 버튼이 가리키는 캐릭터 ID")]
    public int charId;

    [Header("목록 썸네일")]
    public Image iconImage;

    [SerializeField] private Button button;

    private Sprite _runtimeSprite;
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

        var texture = ResourceManager.Instance.Get<Texture2D>(data.card_imageName);
        if (texture == null)
        {
            Debug.LogWarning($"[CharacterButtonView] Texture 로드 실패: {data.card_imageName}");
            return;
        }

        if (_runtimeSprite != null) Destroy(_runtimeSprite);

        _runtimeSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        iconImage.sprite = _runtimeSprite;
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

    private void OnDestroy()
    {
        if (_runtimeSprite != null) Destroy(_runtimeSprite);
    }
}

