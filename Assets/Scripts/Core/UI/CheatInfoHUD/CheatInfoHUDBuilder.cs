using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public static class CheatInfoHUDBuilder
    {
        private const string ScoreHudPrefabPath = "Prefabs/UI/HUDs/ScoreHUD";

        public static CheatInfoHUD Create(Transform parent)
        {
            GameObject root = new GameObject(
                nameof(CheatInfoHUD),
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = NewUIObject("panel-info", root.transform);
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(24f, -24f);
            panel.sizeDelta = new Vector2(360f, 96f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.04f, 0.07f, 0.58f);
            panelImage.raycastTarget = false;

            TMP_Text text = CreateText(panel);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            return root.AddComponent<CheatInfoHUD>();
        }

        private static TMP_Text CreateText(Transform parent)
        {
            RectTransform textRect = NewUIObject("txt-info", parent);
            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_Text templateText = GetScoreHudText();
            if (templateText != null)
            {
                text.font = templateText.font;
                text.fontSharedMaterial = templateText.fontSharedMaterial;
            }

            text.text = "Speed: 0.00\nDifficulty: Easy";
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text GetScoreHudText()
        {
            GameObject prefab = Resources.Load<GameObject>(ScoreHudPrefabPath);
            return prefab != null ? prefab.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private static RectTransform NewUIObject(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
