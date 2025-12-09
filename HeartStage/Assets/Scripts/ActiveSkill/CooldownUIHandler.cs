using UnityEngine;
using UnityEngine.UI;

public class CooldownUIHandler
{
    private GameObject caster;
    private Slider slider;
    private RectTransform sliderRect;
    private Canvas canvas;
    private Vector3 worldOffset;

    public CooldownUIHandler(GameObject caster, Slider sliderPrefab, Canvas canvas, Vector3 worldOffset = default)
    {
        this.caster = caster;
        this.canvas = canvas;
        this.worldOffset = worldOffset == default ? new Vector3(0f, 2f, 0f) : worldOffset;

        slider = GameObject.Instantiate(sliderPrefab, canvas.transform);
        sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.SetAsFirstSibling();
    }

    public void InitMaxValue(float max)
    {
        slider.maxValue = max;
        slider.value = 0f;
    }

    public void ResetSlider()
    {
        slider.value = 0f;
    }

    public void Show()
    {
        if (slider != null)
            slider.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (slider != null)
            slider.gameObject.SetActive(false);
    }

    public void UpdateUI(float remainTime)
    {
        if (caster == null)
        {
            Dispose();
            return;
        }
        if (slider == null) return;

        float max = slider.maxValue;
        slider.value = Mathf.Clamp(max - remainTime, 0f, max);

        Vector3 worldPos = caster.transform.position + worldOffset;
        Camera cam = Camera.main;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            Hide();
            return;
        }

        Show();

        sliderRect.position = screenPos;
    }

    public void Dispose()
    {
        if (slider != null)
        {
            GameObject.Destroy(slider.gameObject);
            slider = null;
            sliderRect = null;
        }
    }
}