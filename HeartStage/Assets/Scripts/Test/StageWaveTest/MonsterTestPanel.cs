using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MonsterTestPanel : MonoBehaviour
{
    [Header("루트 & 타이틀")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;

    [Header("페이지 버튼")]
    [SerializeField] private Button page1Button;
    [SerializeField] private Button page2Button;

    [SerializeField] private GameObject page1Root;
    [SerializeField] private GameObject page2Root;

    [Header("공통 버튼")]
    [SerializeField] private Button closeButton;

    // ==========================
    // Page1 - 스킬 ID 이전(기본/전투 스탯)
    // ==========================
    [Header("Page1 - 기본 / 전투 스탯")]
    [SerializeField] private TMP_InputField monsterNameInput;
    [SerializeField] private TMP_InputField monTypeInput;       // monsterType
    [SerializeField] private TMP_InputField hpInput;
    [SerializeField] private TMP_InputField attInput;
    [SerializeField] private TMP_InputField atkTypeInput;       // attType
    [SerializeField] private TMP_InputField attackSpeedInput;
    [SerializeField] private TMP_InputField attackRangeInput;
    [SerializeField] private TMP_InputField bulletSpeedInput;
    [SerializeField] private TMP_InputField moveSpeedInput;
    [SerializeField] private TMP_InputField minLevelInput;      // minExp
    [SerializeField] private TMP_InputField maxLevelInput;      // maxExp

    // ==========================
    // Page2 - 스킬 ID 이후(스킬/드랍/프리팹/소환)
    // ==========================
    [Header("Page2 - 스킬 / 드랍 / 프리팹")]
    [SerializeField] private TMP_InputField skillId1Input;
    [SerializeField] private TMP_InputField skillId2Input;
    [SerializeField] private TMP_InputField skillId3Input;

    [SerializeField] private TMP_InputField itemId1Input;
    [SerializeField] private TMP_InputField dropCount1Input;
    [SerializeField] private TMP_InputField itemId2Input;
    [SerializeField] private TMP_InputField dropCount2Input;

    [SerializeField] private TMP_InputField prefab1Input;       // 몬스터 소환용 이름
    [SerializeField] private Button prefab1SpawnButton;         // prefab1 옆 소환 버튼
    [SerializeField] private TMP_InputField prefab2Input;       // 아이콘 이름

    [Header("프리뷰 소환 위치")]
    [SerializeField] private Transform previewPoint;

    [Header("프리뷰 정렬 설정")]
    [SerializeField] private bool overrideSorting = true;
    [SerializeField] private string previewSortingLayerName = "Default";
    [SerializeField] private int previewSortingOrder = 500;
    // ==========================
    // 내부 상태
    // ==========================
    private MonsterData currentMonster;
    private int currentPage = 1;
    private System.Action onClosed;

    // 프리뷰용
    private GameObject currentPreviewInstance;
    private bool hasHandle;
    private AsyncOperationHandle<GameObject> currentHandle;

    private void Awake()
    {
        if (page1Button != null) page1Button.onClick.AddListener(() => ShowPage(1));
        if (page2Button != null) page2Button.onClick.AddListener(() => ShowPage(2));
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (prefab1SpawnButton != null)
            prefab1SpawnButton.onClick.AddListener(OnSpawnButtonClicked);

        HookInputEvents();
        Hide();
    }

    private void OnDestroy()
    {
        ReleasePreview();
    }

    // ==========================
    // 공개 API
    // ==========================

    public void Open(MonsterData monster, System.Action onClosed = null)
    {
        this.currentMonster = monster;
        this.onClosed = onClosed;

        if (currentMonster == null)
        {
            Hide();
            return;
        }

        SyncMonsterToUI();
        ShowPage(1);

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        ReleasePreview();
        if (root != null)
            root.SetActive(false);

        currentMonster = null;
    }

    private void Close()
    {
        ReleasePreview();  // 🔹 프리뷰 제거
        Hide();
        onClosed?.Invoke();
    }

    // ==========================
    // 페이지 전환
    // ==========================

    private void ShowPage(int page)
    {
        currentPage = page;

        if (page1Root != null) page1Root.SetActive(page == 1);
        if (page2Root != null) page2Root.SetActive(page == 2);

        if (page1Button != null) page1Button.interactable = page != 1;
        if (page2Button != null) page2Button.interactable = page != 2;
    }

    // ==========================
    // 데이터 → UI 동기화
    // ==========================

    private void SyncMonsterToUI()
    {
        if (currentMonster == null) return;

        // Page1
        monsterNameInput?.SetTextWithoutNotify(currentMonster.monsterName ?? "");
        monTypeInput?.SetTextWithoutNotify(currentMonster.monsterType.ToString());
        hpInput?.SetTextWithoutNotify(currentMonster.hp.ToString());
        attInput?.SetTextWithoutNotify(currentMonster.att.ToString());
        atkTypeInput?.SetTextWithoutNotify(currentMonster.attType.ToString());
        attackSpeedInput?.SetTextWithoutNotify(currentMonster.attackSpeed.ToString());
        attackRangeInput?.SetTextWithoutNotify(currentMonster.attackMinRange.ToString());
        bulletSpeedInput?.SetTextWithoutNotify(currentMonster.bulletSpeed.ToString());
        moveSpeedInput?.SetTextWithoutNotify(currentMonster.moveSpeed.ToString("0.00", CultureInfo.InvariantCulture));
        minLevelInput?.SetTextWithoutNotify(currentMonster.minExp.ToString());
        maxLevelInput?.SetTextWithoutNotify(currentMonster.maxExp.ToString());

        // Page2
        skillId1Input?.SetTextWithoutNotify(currentMonster.skillId1.ToString());
        skillId2Input?.SetTextWithoutNotify(currentMonster.skillId2.ToString());
        skillId3Input?.SetTextWithoutNotify(currentMonster.skillId3.ToString());

        itemId1Input?.SetTextWithoutNotify(currentMonster.itemId1.ToString());
        dropCount1Input?.SetTextWithoutNotify(currentMonster.dropCount1.ToString());
        itemId2Input?.SetTextWithoutNotify(currentMonster.itemId2.ToString());
        dropCount2Input?.SetTextWithoutNotify(currentMonster.dropCount2.ToString());

        prefab1Input?.SetTextWithoutNotify(currentMonster.prefab1 ?? "");
        prefab2Input?.SetTextWithoutNotify(currentMonster.prefab2 ?? "");
    }

    // ==========================
    // UI 이벤트(입력 → 데이터)
    // ==========================

    private void HookInputEvents()
    {
        // Page1
        if (monsterNameInput != null) monsterNameInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.monsterName = v;
            MarkDirty();
        });

        if (monTypeInput != null) monTypeInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.monsterType = ParseInt(v, currentMonster.monsterType);
            monTypeInput.SetTextWithoutNotify(currentMonster.monsterType.ToString());
            MarkDirty();
        });

        if (hpInput != null) hpInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.hp = ParseInt(v, currentMonster.hp);
            hpInput.SetTextWithoutNotify(currentMonster.hp.ToString());
            MarkDirty();
        });

        if (attInput != null) attInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.att = ParseInt(v, currentMonster.att);
            attInput.SetTextWithoutNotify(currentMonster.att.ToString());
            MarkDirty();
        });

        if (atkTypeInput != null) atkTypeInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.attType = ParseInt(v, currentMonster.attType);
            atkTypeInput.SetTextWithoutNotify(currentMonster.attType.ToString());
            MarkDirty();
        });

        if (attackSpeedInput != null) attackSpeedInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.attackSpeed = ParseFloat(v, currentMonster.attackSpeed);
            attackSpeedInput.SetTextWithoutNotify(currentMonster.attackSpeed.ToString());
            MarkDirty();
        });

        if (attackRangeInput != null) attackRangeInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.attackMinRange = ParseFloat(v, currentMonster.attackMinRange);
            attackRangeInput.SetTextWithoutNotify(currentMonster.attackMinRange.ToString());
            MarkDirty();
        });

        if (bulletSpeedInput != null) bulletSpeedInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.bulletSpeed = ParseInt(v, currentMonster.bulletSpeed);
            bulletSpeedInput.SetTextWithoutNotify(currentMonster.bulletSpeed.ToString());
            MarkDirty();
        });

        if (moveSpeedInput != null) moveSpeedInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.moveSpeed = ParseFloat(v, currentMonster.moveSpeed);
            moveSpeedInput.SetTextWithoutNotify(currentMonster.moveSpeed.ToString("0.00", CultureInfo.InvariantCulture));
            MarkDirty();
        });

        if (minLevelInput != null) minLevelInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.minExp = ParseInt(v, currentMonster.minExp);
            minLevelInput.SetTextWithoutNotify(currentMonster.minExp.ToString());
            MarkDirty();
        });

        if (maxLevelInput != null) maxLevelInput.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.maxExp = ParseInt(v, currentMonster.maxExp);
            maxLevelInput.SetTextWithoutNotify(currentMonster.maxExp.ToString());
            MarkDirty();
        });

        // Page2
        if (skillId1Input != null) skillId1Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.skillId1 = ParseInt(v, currentMonster.skillId1);
            skillId1Input.SetTextWithoutNotify(currentMonster.skillId1.ToString());
            MarkDirty();
        });

        if (skillId2Input != null) skillId2Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.skillId2 = ParseInt(v, currentMonster.skillId2);
            skillId2Input.SetTextWithoutNotify(currentMonster.skillId2.ToString());
            MarkDirty();
        });

        if (skillId3Input != null) skillId3Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.skillId3 = ParseInt(v, currentMonster.skillId3);
            skillId3Input.SetTextWithoutNotify(currentMonster.skillId3.ToString());
            MarkDirty();
        });

        if (itemId1Input != null) itemId1Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.itemId1 = ParseInt(v, currentMonster.itemId1);
            itemId1Input.SetTextWithoutNotify(currentMonster.itemId1.ToString());
            MarkDirty();
        });

        if (dropCount1Input != null) dropCount1Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.dropCount1 = ParseInt(v, currentMonster.dropCount1);
            dropCount1Input.SetTextWithoutNotify(currentMonster.dropCount1.ToString());
            MarkDirty();
        });

        if (itemId2Input != null) itemId2Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.itemId2 = ParseInt(v, currentMonster.itemId2);
            itemId2Input.SetTextWithoutNotify(currentMonster.itemId2.ToString());
            MarkDirty();
        });

        if (dropCount2Input != null) dropCount2Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.dropCount2 = ParseInt(v, currentMonster.dropCount2);
            dropCount2Input.SetTextWithoutNotify(currentMonster.dropCount2.ToString());
            MarkDirty();
        });

        if (prefab1Input != null) prefab1Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.prefab1 = v;
            MarkDirty();
        });

        if (prefab2Input != null) prefab2Input.onEndEdit.AddListener(v =>
        {
            if (currentMonster == null) return;
            currentMonster.prefab2 = v;
            MarkDirty();
        });
    }

    // ==========================
    // 소환 관련
    // ==========================

    private void OnSpawnButtonClicked()
    {
        if (currentMonster == null)
        {
            Debug.LogWarning("[MonsterTest] currentMonster 가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(currentMonster.prefab1))
        {
            Debug.LogWarning("[MonsterTest] prefab1 이 비어있습니다.");
            return;
        }

        if (previewPoint == null)
        {
            Debug.LogWarning("[MonsterTest] previewPoint 가 설정되어 있지 않습니다.");
            return;
        }

        ReleasePreview();

        Debug.Log($"[MonsterTest] 몬스터 프리뷰 소환 시도: {currentMonster.prefab1}");

        var handle = Addressables.InstantiateAsync(
            currentMonster.prefab1,
            previewPoint.position,
            Quaternion.identity
        );

        hasHandle = true;
        currentHandle = handle;

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentPreviewInstance = op.Result;

                SetupPreviewSorting(currentPreviewInstance);
            }
            else
            {
                Debug.LogWarning($"[MonsterTest] 몬스터 소환 실패: {currentMonster.prefab1}");
            }
        };
    }
    private void SetupPreviewSorting(GameObject go)
    {
        if (!overrideSorting || go == null)
            return;

        // 1) SpriteRenderer들 정렬
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            if (!string.IsNullOrEmpty(previewSortingLayerName))
                sr.sortingLayerName = previewSortingLayerName;

            sr.sortingOrder = previewSortingOrder;
        }

        // 2) World Space Canvas 있으면 같이 올려주기 (있을 수도 있어서)
        var canvases = go.GetComponentsInChildren<Canvas>(true);
        foreach (var cv in canvases)
        {
            if (cv.renderMode == RenderMode.WorldSpace)
            {
                cv.overrideSorting = true;
                if (!string.IsNullOrEmpty(previewSortingLayerName))
                    cv.sortingLayerName = previewSortingLayerName;

                cv.sortingOrder = previewSortingOrder + 1;
            }
        }
    }

    private void ReleasePreview()
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        if (hasHandle)
        {
            Addressables.Release(currentHandle);
            hasHandle = false;
        }
    }

    // ==========================
    // 유틸
    // ==========================

    private int ParseInt(string text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        if (int.TryParse(text, out int v)) return v;
        return fallback;
    }

    private float ParseFloat(string text, float fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        text = text.Replace(',', '.');
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            return v;
        return fallback;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (currentMonster != null)
            UnityEditor.EditorUtility.SetDirty(currentMonster);
#endif
    }
}
