using UnityEngine;

public class DropItem : MonoBehaviour
{
    [Header("Size Normalize")]
    [SerializeField] private Sprite referenceSprite;

    [HideInInspector]
    public int itemId;
    [HideInInspector]
    public int amount;
    [HideInInspector]
    public Vector3 targetPos;

    private Vector3 startPos;
    private float delayTime = 1f;
    private float flyTime = 0.5f;
    private float timer = 0f;
    private bool isFlying = false;

    private SpriteRenderer spriteRenderer;
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        timer = 0f;
        isFlying = false; 
        spriteRenderer.transform.localScale = Vector3.one;
    }

    public void Setup(int id, int amt, Vector3 spawnPos, Vector3 target)
    {
        this.itemId = id;
        this.amount = amt;
        transform.position = spawnPos;
        this.targetPos = target;

        // sprite setting
        if(id == ItemID.LightStick)
        {
            spriteRenderer.sprite = ResourceManager.Instance.GetSprite("DropLightstickImage");
        }
        else
        {
            spriteRenderer.sprite = ResourceManager.Instance.GetSprite(DataTableManager.ItemTable.Get(id).prefab);
        }
        NormalizeSpriteSize(); 
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // delay time
        if (!isFlying)
        {
            if (timer >= delayTime)
            {
                isFlying = true;
                timer = 0f;
                startPos = transform.position;
            }
            return;
        }

        // flying
        float t = timer / flyTime;
        if (t >= 1f)
        {
            transform.position = targetPos;

            // 회수 + 아이템 사용
            PoolManager.Instance.Release(ItemManager.ItemPoolId, gameObject);
            ItemManager.Instance.UseItem(itemId, amount);
            return;
        }

        transform.position = Vector3.Lerp(startPos, targetPos, t);
    }

    private void NormalizeSpriteSize()
    {
        if (referenceSprite == null || spriteRenderer.sprite == null)
            return;

        float refPixels = Mathf.Max(
            referenceSprite.rect.width,
            referenceSprite.rect.height
        );

        float curPixels = Mathf.Max(
            spriteRenderer.sprite.rect.width,
            spriteRenderer.sprite.rect.height
        );

        if (curPixels <= 0f)
            return;

        float ratio = refPixels / curPixels;
        spriteRenderer.transform.localScale = Vector3.one * ratio;
    }
}