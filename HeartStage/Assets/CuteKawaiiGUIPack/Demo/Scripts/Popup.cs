// Copyright (C) 2024 ricimi. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at https://unity.com/legal/as-terms.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ricimi
{
    // This class is responsible for popup management. Popups follow the traditional behavior of
    // automatically blocking the input on elements behind it and adding a background texture.
    public class Popup : MonoBehaviour
    {
        public Color backgroundColor = new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f);

        public float destroyTime = 0.5f;

        private GameObject m_background;

        private Coroutine closeRoutine;

        public void Open()
        {
            // close 코루틴 실행 중이면 중지
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            gameObject.SetActive(true);

            var animator = GetComponent<Animator>();
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play("Open", 0, 0f);

            AddBackground();
        }

        public void Close()
        {
            var animator = GetComponent<Animator>();

            animator.Play("Close", 0, 0f);

            RemoveBackground();

            if (closeRoutine != null)
                StopCoroutine(closeRoutine);

            closeRoutine = StartCoroutine(CloseAfterAnimation());
        }

        private IEnumerator CloseAfterAnimation()
        {
            yield return new WaitForSecondsRealtime(destroyTime); // 애니메이션이 끝나는 시간
            Destroy(m_background);                        // 배경은 제거
            gameObject.SetActive(false);                  // 팝업은 삭제 X, 숨기기만
        }

        private void AddBackground()
        {
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, backgroundColor);
            bgTex.Apply();

            m_background = new GameObject("PopupBackground");
            var image = m_background.AddComponent<Image>();
            var rect = new Rect(0, 0, bgTex.width, bgTex.height);
            var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
            // Clone the material, which is the default UI material, to avoid changing it permanently.
            image.material = new Material(image.material);
            image.material.mainTexture = bgTex;
            image.sprite = sprite;
            var newColor = image.color;
            image.color = newColor;
            image.canvasRenderer.SetAlpha(0.0f);
            image.CrossFadeAlpha(1.0f, 0.4f, false);

            var canvas = GetComponentInParent<Canvas>();
            m_background.transform.localScale = new Vector3(1, 1, 1);
            m_background.GetComponent<RectTransform>().sizeDelta = canvas.GetComponent<RectTransform>().sizeDelta;
            m_background.transform.SetParent(canvas.transform, false);
            m_background.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        private void RemoveBackground()
        {
            var image = m_background.GetComponent<Image>();
            if (image != null)
            {
                image.CrossFadeAlpha(0.0f, 0.2f, false);
            }
        }
    }
}
