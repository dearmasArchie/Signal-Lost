using SignalLost.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase4Ui
    {
        private const string UiRoot = "UI/";
        private const string FontRoot = "Fonts/";

        public static Font BodyFont => LoadFont("KenneyFuture");

        public static Font HeaderFont => LoadFont("KenneyFutureNarrow");

        public static Font NumericFont => LoadFont("KenneyMiniSquare");

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static GameObject CreateEventSystem(Transform parent)
        {
            GameObject root = new GameObject("EventSystem");
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            root.AddComponent<EventSystem>();

            InputSystemUIInputModule module = root.AddComponent<InputSystemUIInputModule>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                SignalLostInputActionsBuilder.AssetPath);

            if (actions == null)
            {
                throw new System.InvalidOperationException(
                    $"Missing input actions asset: {SignalLostInputActionsBuilder.AssetPath}");
            }

            module.actionsAsset = actions;
            module.point = GetUiActionReference(actions, "Point");
            module.leftClick = GetUiActionReference(actions, "Click");
            module.rightClick = GetUiActionReference(actions, "RightClick");
            module.middleClick = GetUiActionReference(actions, "MiddleClick");
            module.scrollWheel = GetUiActionReference(actions, "ScrollWheel");
            module.move = GetUiActionReference(actions, "Navigate");
            module.submit = GetUiActionReference(actions, "Submit");
            module.cancel = GetUiActionReference(actions, "Cancel");
            module.trackedDevicePosition = GetUiActionReference(actions, "TrackedDevicePosition");
            module.trackedDeviceOrientation = GetUiActionReference(actions, "TrackedDeviceOrientation");

            root.SetActive(true);
            return root;
        }

        private static InputActionReference GetUiActionReference(InputActionAsset actions, string actionName)
        {
            const string folder = "Assets/Generated/Input";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Generated", "Input");
            }

            string path = $"{folder}/UI_{actionName}.asset";
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);

            if (reference == null)
            {
                reference = ScriptableObject.CreateInstance<InputActionReference>();
                AssetDatabase.CreateAsset(reference, path);
            }

            reference.Set(actions, "UI", actionName);
            EditorUtility.SetDirty(reference);
            AssetDatabase.SaveAssetIfDirty(reference);
            return reference;
        }

        public static Image CreateFullScreenImage(Transform parent, string name, Color color, bool raycastTarget)
        {
            RectTransform rect = CreateRect(
                parent,
                name,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            string spriteName,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string content,
            Font font,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = font != null ? font : BodyFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize = 28)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("ButtonRect");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.16f, 0.34f, 0.4f, 0.95f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.7f, 0.9f, 0.95f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.95f, 0.9f, 1f);
            colors.pressedColor = new Color(0.5f, 0.7f, 0.75f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.4f, 0.45f, 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Text text = CreateText(
                rect,
                "Label",
                label,
                BodyFont,
                fontSize,
                new Color(0.9f, 0.98f, 1f),
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            text.rectTransform.offsetMin = new Vector2(12f, 4f);
            text.rectTransform.offsetMax = new Vector2(-12f, -4f);

            rect.gameObject.AddComponent<UiButtonSfx>();
            return button;
        }

        public static void SetOnClick(Button button, UnityAction action)
        {
            if (button != null && action != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
            }
        }

        public static void AddTextShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        public static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>(UiRoot + name);
        }

        public static Font LoadFont(string name)
        {
            Font font = Resources.Load<Font>(FontRoot + name);
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
