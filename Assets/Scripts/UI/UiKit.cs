using System;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Uncrowned.UI
{
    /// <summary>Small helpers to build uGUI screens in code (menus, map, town prompts).</summary>
    public static class UiKit
    {
        static Font font;
        public static Font Font => font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Color Gold = new(1f, 0.82f, 0.35f);
        public static readonly Color Panel = new(0.07f, 0.05f, 0.1f, 0.92f);
        public static readonly Color Muted = new(0.72f, 0.68f, 0.8f);

        public static Transform Canvas(string name, int order = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Stretches to fill the parent.</summary>
        public static Image Fill(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.Square;
            img.color = color;
            return img;
        }

        public static Image Image(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite = null)
        {
            var img = Rect(parent, name, anchor, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : SpriteFactory.Square;
            img.color = color;
            return img;
        }

        public static Text Text(Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
        {
            var t = Rect(parent, "Text", anchor, pos, box).gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? Color.white;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize = 28)
        {
            var img = Image(parent, "Button " + label, new Color(0.16f, 0.12f, 0.22f, 0.95f), anchor, pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.4f, 1.3f, 1.6f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            var t = Text(img.transform, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            t.color = Gold;
            return btn;
        }
    }
}
