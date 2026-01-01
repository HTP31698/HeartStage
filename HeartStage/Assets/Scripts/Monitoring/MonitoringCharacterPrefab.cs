using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
public class MonitoringCharacterPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dipatchCount;
    [SerializeField] private Image borderImage;

    [SerializeField] private Color normalBorderColor = Color.white;
    [SerializeField] private Color selectedBorderColor = Color.yellow;
    [SerializeField] private Color lockedBorderColor = Color.gray;
    [SerializeField] private Color disabledBorderColor = Color.red;

    private const int DISPATCH_LIMIT = 2;
    private int currentDispatchCount = DISPATCH_LIMIT;

    // 드래그 관련 변수들
    private CharacterData characterData;
    private MonitoringCharacterSelectUI parentUI;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GameObject dragIcon;
    private bool isLocked = false;
    private bool isHighlighted = false;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rectTransform = GetComponent<RectTransform>();
    }

    public void Init(CharacterData characterData)
    {
        this.characterData = characterData;
        if (characterData == null) return;

        LoadDispatchCountFromServer();
        SetCharacterName();
        SetCharacterImage();
        UpdateDispatchCountUI();

        parentUI = GetComponentInParent<MonitoringCharacterSelectUI>();
        UpdateVisualState();
    }

    public CharacterData GetCharacterData() => characterData;
    public bool IsLocked() => isLocked;
    public bool CanDispatch() => currentDispatchCount > 0;
    public int GetCurrentDispatchCount() => currentDispatchCount;

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateVisualState();
    }

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        UpdateVisualState();
    }

    public bool ConsumeDispatch()
    {
        if (currentDispatchCount <= 0) return false;

        currentDispatchCount--;
        SaveDispatchCountToServer();
        UpdateDispatchCountUI();
        UpdateVisualState();
        return true;
    }

    private void SetCharacterName()
    {
        if (characterNameText != null)
        {
            characterNameText.text = characterData.char_name;
        }
    }

    private void SetCharacterImage()
    {
        LoadPhotocardAsync(characterData).Forget();
    }

    private async UniTaskVoid LoadPhotocardAsync(CharacterData characterData)
    {
        string charCode = PhotocardHelper.ExtractCharCode(characterData.char_id);
        // 스테이지 배치 UI에서는 Frame 버전 사용
        var sprite = await PhotocardHelper.LoadDisplaySpriteWithFrame(charCode);

        if (sprite != null)
        {
            characterImage.sprite = sprite;
        }
        else
        {
            // fallback: 기존 방식
            characterImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
        }
    }

    private void UpdateDispatchCountUI()
    {
        if (dipatchCount == null) return;

        dipatchCount.text = $"{currentDispatchCount}/{DISPATCH_LIMIT}";
        dipatchCount.color = currentDispatchCount <= 0 ? Color.red : Color.white;
    }

    private void UpdateVisualState()
    {
        bool isDisabled = currentDispatchCount <= 0;
        UpdateCanvasGroup(isDisabled);
        UpdateBorderColor(isDisabled);
    }

    private void UpdateCanvasGroup(bool isDisabled)
    {
        if (canvasGroup == null) return;

        if (isDisabled)
        {
            canvasGroup.alpha = 0.3f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (isLocked)
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }
        else if (isHighlighted)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void UpdateBorderColor(bool isDisabled)
    {
        if (borderImage == null) return;

        Color targetColor = isDisabled ? disabledBorderColor :
                           isLocked ? lockedBorderColor :
                           isHighlighted ? selectedBorderColor :
                           normalBorderColor;

        borderImage.color = targetColor;
    }

    #region 드래그 구현

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (characterData == null || canvas == null || isLocked || currentDispatchCount <= 0)
            return;

        SetHighlighted(true);
        dragIcon = MonitoringDragHelper.CreateDragIcon(canvas, characterImage, rectTransform, eventData, "CharacterDragIcon");

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        MonitoringDragHelper.UpdateDragIconPosition(dragIcon, canvas, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MonitoringDragHelper.DestroyDragIcon(ref dragIcon);

        if (!isLocked)
        {
            SetHighlighted(false);
        }

        if (canvasGroup != null && !isLocked)
        {
            canvasGroup.alpha = 1.0f;
        }
    }

    #endregion

    #region 서버 데이터 관리

    private void LoadDispatchCountFromServer()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData.characterDispatchCounts == null)
        {
            saveData.characterDispatchCounts = new Dictionary<int, int>();
        }

        // 날짜 체크 추가
        CheckAndResetDaily();

        int charId = characterData.char_id;
        if (saveData.characterDispatchCounts.ContainsKey(charId))
        {
            currentDispatchCount = saveData.characterDispatchCounts[charId];
        }
        else
        {
            currentDispatchCount = DISPATCH_LIMIT;
            saveData.characterDispatchCounts[charId] = currentDispatchCount;
        }

        currentDispatchCount = Mathf.Max(0, currentDispatchCount);
    }

    private void CheckAndResetDaily()
    {
        var saveData = SaveLoadManager.Data;

        DateTime now;
        try
        {
            now = FirebaseTime.GetServerTime();
        }
        catch
        {
            now = DateTime.Now;
        }

        string todayKey = now.ToString("yyyyMMdd");

        if (string.IsNullOrEmpty(saveData.lastDispatchResetDate) || saveData.lastDispatchResetDate != todayKey)
        {
            // 자정이 지났으면 모든 캐릭터 파견 횟수 리셋
            if (saveData.characterDispatchCounts != null)
            {
                saveData.characterDispatchCounts.Clear();
            }

            saveData.lastDispatchResetDate = todayKey;
            SaveLoadManager.SaveToServer().Forget();
        }
    }

    private void SaveDispatchCountToServer()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData.characterDispatchCounts == null)
        {
            saveData.characterDispatchCounts = new Dictionary<int, int>();
        }

        saveData.characterDispatchCounts[characterData.char_id] = currentDispatchCount;
    }

    #endregion
}