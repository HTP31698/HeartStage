using TMPro;
using UnityEngine;

public class TutorialScriptPrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tutorialText;

    public void SetTutorialText(string text)
    {
        if (tutorialText != null)
        {
            tutorialText.text = text;
        }
    }
}