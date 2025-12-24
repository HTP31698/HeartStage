using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Image 컴포넌트의 Source Image(Sprite)가 변경될 때 자동으로 RectTransform 크기를 맞춰주는 에디터 확장
/// </summary>
[InitializeOnLoad]
public class ImageSizeAutoFitter : EditorWindow
{
    // 이전 프레임의 Sprite 상태를 저장
    private static Dictionary<int, Sprite> previousSprites = new Dictionary<int, Sprite>();

    static ImageSizeAutoFitter()
    {
        // Selection 변경 시 추적 시작
        Selection.selectionChanged += OnSelectionChanged;
        // 매 프레임 체크
        EditorApplication.update += CheckSpriteChanges;
    }

    private static void OnSelectionChanged()
    {
        // 선택된 오브젝트의 Image 컴포넌트 Sprite 상태 저장
        previousSprites.Clear();
        foreach (var obj in Selection.gameObjects)
        {
            var image = obj.GetComponent<Image>();
            if (image != null)
            {
                previousSprites[image.GetInstanceID()] = image.sprite;
            }
        }
    }

    private static void CheckSpriteChanges()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var image = obj.GetComponent<Image>();
            if (image == null) continue;

            int id = image.GetInstanceID();
            Sprite currentSprite = image.sprite;

            // 이전 상태가 없으면 등록만
            if (!previousSprites.ContainsKey(id))
            {
                previousSprites[id] = currentSprite;
                continue;
            }

            Sprite prevSprite = previousSprites[id];

            // Sprite가 변경되었고, 새 Sprite가 null이 아닌 경우 (None → Sprite 포함)
            if (currentSprite != prevSprite && currentSprite != null)
            {
                ApplyNativeSizeToImage(image);
            }

            // 항상 현재 상태 업데이트
            previousSprites[id] = currentSprite;
        }
    }

    private static void ApplyNativeSizeToImage(Image image)
    {
        Undo.RecordObject(image.rectTransform, "Auto Fit Image Size");

        Rect spriteRect = image.sprite.rect;
        image.rectTransform.sizeDelta = new Vector2(spriteRect.width, spriteRect.height);

        Debug.Log($"[AutoFit] {image.gameObject.name}: {spriteRect.width} x {spriteRect.height}");
    }

    [MenuItem("Tools/Image Size Auto Fitter")]
    public static void ShowWindow()
    {
        GetWindow<ImageSizeAutoFitter>("Image Size Fitter");
    }

    [MenuItem("GameObject/UI/Fit Image to Native Size %&d", false, 0)]
    public static void FitSelectedImageToNativeSize()
    {
        FitSelectedImages();
    }

    private static void FitSelectedImages()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int fittedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) continue;

            Undo.RecordObject(rt, "Fit to Size");

            // 부모 RectTransform 가져오기
            RectTransform parentRt = rt.parent as RectTransform;
            if (parentRt == null) continue;

            // 현재 월드 코너 위치 가져오기
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // corners[0] = bottom-left, corners[1] = top-left, corners[2] = top-right, corners[3] = bottom-right

            Vector3[] parentCorners = new Vector3[4];
            parentRt.GetWorldCorners(parentCorners);

            // 부모 기준 normalized 위치 계산
            float parentWidth = parentCorners[2].x - parentCorners[0].x;
            float parentHeight = parentCorners[2].y - parentCorners[0].y;

            if (parentWidth <= 0 || parentHeight <= 0) continue;

            float anchorMinX = (corners[0].x - parentCorners[0].x) / parentWidth;
            float anchorMinY = (corners[0].y - parentCorners[0].y) / parentHeight;
            float anchorMaxX = (corners[2].x - parentCorners[0].x) / parentWidth;
            float anchorMaxY = (corners[2].y - parentCorners[0].y) / parentHeight;

            // 앵커를 네 모서리에 맞추기
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);

            // offsetMin/Max를 0으로 (앵커가 모서리에 딱 맞음)
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fittedCount++;
            Debug.Log($"[ImageSizeAutoFitter] {obj.name}: 앵커 고정 완료");
        }

        if (fittedCount > 0)
        {
            Debug.Log($"[ImageSizeAutoFitter] {fittedCount}개 오브젝트 크기 적용 완료");
        }
        else
        {
            Debug.LogWarning("[ImageSizeAutoFitter] 선택된 오브젝트에 RectTransform이 없습니다.");
        }
    }

    private float scaleFactor = 1f;

    private void OnGUI()
    {
        GUILayout.Label("Image Size Auto Fitter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Sprite 변경 시 자동으로 크기가 조정됩니다.", MessageType.Info);

        GUILayout.Space(10);

        // 선택된 오브젝트 정보
        GameObject[] selectedObjects = Selection.gameObjects;
        EditorGUILayout.LabelField("선택된 오브젝트:", selectedObjects.Length.ToString());

        GUILayout.Space(10);

        // 옵션
        scaleFactor = EditorGUILayout.FloatField("스케일 배율:", scaleFactor);

        GUILayout.Space(10);

        // 선택된 이미지 미리보기
        if (selectedObjects.Length > 0)
        {
            GUILayout.Label("선택된 이미지 정보:", EditorStyles.boldLabel);

            foreach (GameObject obj in selectedObjects)
            {
                Image image = obj.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    Rect spriteRect = image.sprite.rect;
                    RectTransform rt = image.rectTransform;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("오브젝트:", obj.name);
                    EditorGUILayout.LabelField("스프라이트:", image.sprite.name);
                    EditorGUILayout.LabelField("원본 크기:", $"{spriteRect.width} x {spriteRect.height}");
                    EditorGUILayout.LabelField("현재 크기:", $"{rt.sizeDelta.x} x {rt.sizeDelta.y}");
                    EditorGUILayout.LabelField("적용될 크기:", $"{spriteRect.width * scaleFactor} x {spriteRect.height * scaleFactor}");
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }
        }

        GUILayout.Space(10);

        // 버튼
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("원본 크기로 설정", GUILayout.Height(30)))
        {
            ApplyNativeSize();
        }

        if (GUILayout.Button("스케일 적용", GUILayout.Height(30)))
        {
            ApplyScaledSize();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button("Set Native Size (Unity 기본)", GUILayout.Height(25)))
        {
            SetNativeSizeBuiltIn();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("우클릭 메뉴: GameObject > UI > Fit Image to Native Size", MessageType.Info);
    }

    private void ApplyNativeSize()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int fittedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Image image = obj.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                Undo.RecordObject(image.rectTransform, "Apply Native Size");

                Rect spriteRect = image.sprite.rect;
                image.rectTransform.sizeDelta = new Vector2(spriteRect.width, spriteRect.height);

                fittedCount++;
            }
        }

        if (fittedCount > 0)
        {
            Debug.Log($"[ImageSizeAutoFitter] {fittedCount}개 이미지에 원본 크기 적용 완료");
        }
    }

    private void ApplyScaledSize()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int fittedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Image image = obj.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                Undo.RecordObject(image.rectTransform, "Apply Scaled Size");

                Rect spriteRect = image.sprite.rect;
                image.rectTransform.sizeDelta = new Vector2(
                    spriteRect.width * scaleFactor,
                    spriteRect.height * scaleFactor
                );

                fittedCount++;
            }
        }

        if (fittedCount > 0)
        {
            Debug.Log($"[ImageSizeAutoFitter] {fittedCount}개 이미지에 스케일({scaleFactor}x) 적용 완료");
        }
    }

    private void SetNativeSizeBuiltIn()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int fittedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            Image image = obj.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                Undo.RecordObject(image.rectTransform, "Set Native Size");
                image.SetNativeSize();
                fittedCount++;
            }
        }

        if (fittedCount > 0)
        {
            Debug.Log($"[ImageSizeAutoFitter] {fittedCount}개 이미지에 SetNativeSize() 적용 완료");
        }
    }

    private void OnSelectionChange()
    {
        Repaint();
    }
}
