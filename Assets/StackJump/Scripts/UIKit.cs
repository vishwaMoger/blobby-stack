using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StackJump
{
    public enum Icon { Play, Left, Right, Sound, Mute, Home, Retry, Trophy, Video, Lock }

    // Procedural sprites, icons and chunky game buttons for the code-built UI.
    public static class UIKit
    {
        static Font font;
        static Sprite rounded, circle;
        static readonly Dictionary<Icon, Sprite> icons = new();

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeSprite(96, 40, (x, y) => SdRoundBox(x, y, 1f, 1f, 0.84f), true);
        public static Sprite Circle => circle != null ? circle : circle = MakeSprite(128, 0, (x, y) => Mathf.Sqrt(x * x + y * y) - 0.98f, false);

        public static Sprite IconSprite(Icon icon)
        {
            if (icons.TryGetValue(icon, out var s)) return s;
            return icons[icon] = MakeSprite(128, 0, IconShape(icon), false);
        }

        // ---------- Widgets ----------

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter, float outlineScale = 1f)
        {
            var rt = Rect(parent, "Text", anchor, pos, new Vector2(1600, size * 1.5f));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            t.text = text;
            var outline = rt.gameObject.AddComponent<GameOutline>();
            outline.Size = Mathf.Max(2.5f, size / 15f) * outlineScale;
            outline.ShadowOffset = new Vector2(0f, -Mathf.Max(4f, size / 11f) * outlineScale);
            return t;
        }

        // A chunky button: darker base lip, bright face with a gloss strip, then icon and/or label.
        public static GameButton Button(Transform parent, string label, Icon? icon, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, bool round = false)
        {
            var rt = Rect(parent, "Button " + label, anchor, pos, size);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            var sprite = round ? Circle : Rounded;
            var lip = Img(rt, "Lip", sprite, Color.Lerp(color, Color.black, 0.38f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), size, !round);
            var faceRt = Rect(rt, "Face", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var face = faceRt.gameObject.AddComponent<Image>();
            face.sprite = sprite;
            face.type = round ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            face.color = color;
            face.raycastTarget = false;
            Img(faceRt, "Gloss", sprite, new Color(1, 1, 1, 0.22f), new Vector2(0.5f, 1f), new Vector2(0, -size.y * 0.22f), new Vector2(size.x - 24f, size.y * 0.36f), !round);

            float iconSize = Mathf.Min(size.y * 0.52f, 72f);
            bool hasLabel = !string.IsNullOrEmpty(label);
            if (icon.HasValue)
            {
                var ix = hasLabel ? -size.x * 0.5f + size.y * 0.55f : 0f;
                var ic = Img(faceRt, "Icon", IconSprite(icon.Value), Color.white, new Vector2(0.5f, 0.5f), new Vector2(ix, 2f), Vector2.one * iconSize);
                var sh = ic.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.3f);
                sh.effectDistance = new Vector2(0, -4);
            }
            Text text = null;
            if (hasLabel)
            {
                float lx = icon.HasValue ? size.y * 0.25f : 0f;
                text = Label(faceRt, label, (int)(size.y * 0.42f), new Vector2(0.5f, 0.5f), new Vector2(lx, 2f));
            }

            var b = rt.gameObject.AddComponent<GameButton>();
            b.Setup(faceRt, lip.rectTransform, onClick, text);
            return b;
        }

        // ---------- Sprite generation ----------

        static Sprite MakeSprite(int size, int border, Func<float, float, float> sdf, bool sliced)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float pixel = 2f / size;
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f, y = (j + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(0.5f - sdf(x, y) / pixel);
                px[j * size + i] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            var b = sliced ? new Vector4(border, border, border, border) : Vector4.zero;
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        static float SdRoundBox(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - hx + r, qy = Mathf.Abs(y) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float SdBox(float x, float y, float x0, float y0, float x1, float y1) =>
            SdRoundBox(x - (x0 + x1) * 0.5f, y - (y0 + y1) * 0.5f, (x1 - x0) * 0.5f, (y1 - y0) * 0.5f, 0.05f);

        static float SdSegment(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        // Signed distance to a convex polygon (counter-clockwise points).
        static float SdPoly(float x, float y, params Vector2[] pts)
        {
            float d = float.MaxValue;
            bool inside = true;
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Length];
                d = Mathf.Min(d, SdSegment(x, y, a.x, a.y, b.x, b.y, 0f));
                float cross = (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x);
                if (cross < 0) inside = false;
            }
            return inside ? -d : d;
        }

        static float SdRing(float x, float y, float r, float w) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - r) - w;

        static Func<float, float, float> IconShape(Icon icon)
        {
            switch (icon)
            {
                case Icon.Play:
                    return (x, y) => SdPoly(x, y, new Vector2(-0.42f, -0.62f), new Vector2(0.68f, 0f), new Vector2(-0.42f, 0.62f)) - 0.06f;
                case Icon.Left:
                    return (x, y) => Mathf.Min(SdSegment(x, y, 0.28f, 0.58f, -0.3f, 0f, 0.16f), SdSegment(x, y, -0.3f, 0f, 0.28f, -0.58f, 0.16f));
                case Icon.Right:
                    return (x, y) => Mathf.Min(SdSegment(x, y, -0.28f, 0.58f, 0.3f, 0f, 0.16f), SdSegment(x, y, 0.3f, 0f, -0.28f, -0.58f, 0.16f));
                case Icon.Sound:
                case Icon.Mute:
                    return (x, y) =>
                    {
                        float body = Mathf.Min(SdBox(x, y, -0.78f, -0.26f, -0.42f, 0.26f),
                            SdPoly(x, y, new Vector2(-0.45f, -0.26f), new Vector2(0.02f, -0.66f), new Vector2(0.02f, 0.66f), new Vector2(-0.45f, 0.26f)));
                        if (icon == Icon.Mute)
                            return Mathf.Min(body, Mathf.Min(SdSegment(x, y, 0.3f, -0.32f, 0.8f, 0.32f, 0.1f), SdSegment(x, y, 0.3f, 0.32f, 0.8f, -0.32f, 0.1f)));
                        float ang = Mathf.Abs(Mathf.Atan2(y, x - 0.02f));
                        float waves = Mathf.Min(SdRing(x - 0.02f, y, 0.38f, 0.08f), SdRing(x - 0.02f, y, 0.68f, 0.08f));
                        waves = Mathf.Max(waves, ang - 0.85f);
                        waves = Mathf.Max(waves, 0.1f - x);
                        return Mathf.Min(body, waves);
                    };
                case Icon.Home:
                    return (x, y) =>
                    {
                        float roof = SdPoly(x, y, new Vector2(-0.82f, 0.05f), new Vector2(0.82f, 0.05f), new Vector2(0f, 0.78f)) - 0.04f;
                        float house = SdBox(x, y, -0.55f, -0.72f, 0.55f, 0.1f);
                        float door = SdBox(x, y, -0.17f, -0.8f, 0.17f, -0.22f);
                        return Mathf.Max(Mathf.Min(roof, house), -door);
                    };
                case Icon.Retry:
                    return (x, y) =>
                    {
                        float a = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                        float ring = SdRing(x, y, 0.55f, 0.14f);
                        if (a > 15f && a < 75f) ring = 1f;
                        float head = SdPoly(x, y, new Vector2(0.24f, 0.34f), new Vector2(0.72f, 0.18f), new Vector2(0.6f, 0.7f));
                        return Mathf.Min(ring, head);
                    };
                case Icon.Trophy:
                    return (x, y) =>
                    {
                        float cup = Mathf.Max(Mathf.Sqrt(x * x + (y - 0.22f) * (y - 0.22f)) - 0.52f, y - 0.66f);
                        cup = Mathf.Min(cup, SdBox(x, y, -0.52f, 0.22f, 0.52f, 0.66f));
                        float handles = Mathf.Min(SdRing(x + 0.52f, y - 0.36f, 0.2f, 0.07f), SdRing(x - 0.52f, y - 0.36f, 0.2f, 0.07f));
                        float stem = SdBox(x, y, -0.1f, -0.5f, 0.1f, -0.2f);
                        float foot = SdBox(x, y, -0.4f, -0.74f, 0.4f, -0.5f);
                        return Mathf.Min(Mathf.Min(cup, handles), Mathf.Min(stem, foot));
                    };
                case Icon.Video:
                    return (x, y) => Mathf.Min(SdRoundBox(x + 0.18f, y, 0.52f, 0.42f, 0.12f),
                        SdPoly(x, y, new Vector2(0.38f, 0f), new Vector2(0.82f, -0.38f), new Vector2(0.82f, 0.38f)));
                case Icon.Lock:
                    return (x, y) => Mathf.Min(SdRoundBox(x, y + 0.25f, 0.55f, 0.42f, 0.1f),
                        Mathf.Max(SdRing(x, y - 0.18f, 0.34f, 0.1f), -y + 0.1f));
                default:
                    return (x, y) => 1f;
            }
        }
    }

    // Press-down animation and click handling for UIKit buttons.
    public class GameButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform face, lip;
        Action onClick;
        float press, hover;
        bool down;
        public Text Text { get; private set; }
        public bool Pulse;

        public void Setup(RectTransform faceRect, RectTransform lipRect, Action click, Text text)
        {
            face = faceRect;
            lip = lipRect;
            onClick = click;
            Text = text;
        }

        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;
        public void OnPointerEnter(PointerEventData e) => hover = 1f;
        public void OnPointerExit(PointerEventData e) { hover = 0f; down = false; }
        public void OnPointerClick(PointerEventData e) => onClick?.Invoke();

        void Update()
        {
            press = Mathf.MoveTowards(press, down ? 1f : 0f, Time.unscaledDeltaTime * 14f);
            face.anchoredPosition = new Vector2(0, -press * 8f);
            float pulse = Pulse ? 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f : 1f;
            float s = pulse * (1f + hover * 0.04f - press * 0.03f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
