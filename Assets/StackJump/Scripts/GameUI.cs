using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StackJump
{
    // All UI, built in code: HUD score, "+N" popups, PERFECT callouts, menu and game-over screens.
    public class GameUI : MonoBehaviour
    {
        public const string Title = "STACK HOP";

        public Action OnPrevChar, OnNextChar, OnCharacters;

        CanvasScaler scaler;
        RectTransform canvasRect, popupLayer;
        GameObject menu, hud, over;
        Text score, perfect, banner, best, charName, charLock, tapMenu;
        Text overScore, overBest, overNew, overUnlock, tapOver;
        Font font;
        Sprite rounded;
        float scorePunch, perfectT = 9f, bannerT = 9f, overT;

        class Popup { public Text Text; public float T; public Vector2 Start; }
        readonly List<Popup> popups = new();

        static readonly Color Gold = new(1f, 0.89f, 0.36f);

        public void Init()
        {
            font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = MakeRoundedSprite();

            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasRect = (RectTransform)go.transform;

            popupLayer = Panel("Popups").GetComponent<RectTransform>();

            hud = Panel("HUD");
            score = Label(hud, "0", 170, new Vector2(0.5f, 1f), new Vector2(0, -200));
            perfect = Label(hud, "PERFECT!", 84, new Vector2(0.5f, 1f), new Vector2(0, -340));
            perfect.color = Gold;

            menu = Panel("Menu");
            var title = Label(menu, Title, 180, new Vector2(0.5f, 1f), new Vector2(0, -250));
            title.color = Color.white;
            best = Label(menu, "BEST 0", 60, new Vector2(0.5f, 1f), new Vector2(0, -390));
            tapMenu = Label(menu, "TAP TO PLAY", 76, new Vector2(0.5f, 0f), new Vector2(0, 470));
            charName = Label(menu, "", 62, new Vector2(0.5f, 0f), new Vector2(0, 300));
            charLock = Label(menu, "", 44, new Vector2(0.5f, 0f), new Vector2(0, 225));
            charLock.color = Gold;
            Button(menu, "<", new Vector2(0.5f, 0f), new Vector2(-340, 300), () => OnPrevChar?.Invoke());
            Button(menu, ">", new Vector2(0.5f, 0f), new Vector2(340, 300), () => OnNextChar?.Invoke());

            over = Panel("GameOver");
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(over.transform, false);
            Stretch((RectTransform)dim.transform);
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0.05f, 0.02f, 0.12f, 0.35f);
            dimImage.raycastTarget = false;
            Label(over, "SCORE", 56, new Vector2(0.5f, 1f), new Vector2(0, -330));
            overScore = Label(over, "0", 220, new Vector2(0.5f, 1f), new Vector2(0, -480));
            overBest = Label(over, "BEST 0", 64, new Vector2(0.5f, 1f), new Vector2(0, -640));
            overNew = Label(over, "NEW BEST!", 80, new Vector2(0.5f, 1f), new Vector2(0, -760));
            overNew.color = Gold;
            overUnlock = Label(over, "", 50, new Vector2(0.5f, 1f), new Vector2(0, -870));
            overUnlock.color = Gold;
            tapOver = Label(over, "TAP TO PLAY AGAIN", 70, new Vector2(0.5f, 0f), new Vector2(0, 470));
            Button(over, "CHARACTERS", new Vector2(0.5f, 0f), new Vector2(0, 290), () => OnCharacters?.Invoke(), 520);

            banner = Label(canvasRect.gameObject, "", 70, new Vector2(0.5f, 1f), new Vector2(0, -470));

            ShowMenu(0, "", "");
        }

        public void ShowMenu(int bestScore, string name, string lockText)
        {
            menu.SetActive(true);
            hud.SetActive(false);
            over.SetActive(false);
            best.text = bestScore > 0 ? "BEST " + bestScore : "";
            charName.text = name;
            charLock.text = lockText;
        }

        public void ShowHud()
        {
            menu.SetActive(false);
            over.SetActive(false);
            hud.SetActive(true);
            perfectT = 9f;
            SetScore(0, false);
        }

        public void ShowGameOver(int finalScore, int bestScore, bool newBest, string unlock)
        {
            hud.SetActive(false);
            over.SetActive(true);
            overScore.text = finalScore.ToString();
            overBest.text = "BEST " + bestScore;
            overNew.gameObject.SetActive(newBest);
            overUnlock.text = unlock;
            overT = 0f;
        }

        public void SetScore(int value, bool punch = true)
        {
            score.text = value.ToString();
            if (punch) scorePunch = 1f;
        }

        public void Perfect(int streak)
        {
            perfect.text = streak > 1 ? "PERFECT x" + streak + "!" : "PERFECT!";
            perfectT = 0f;
        }

        public void Banner(string text)
        {
            banner.text = text;
            bannerT = 0f;
        }

        public void PopupAt(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            var t = Label(popupLayer.gameObject, text, 96, new Vector2(0.5f, 0.5f), local);
            t.color = color;
            popups.Add(new Popup { Text = t, T = 0f, Start = local });
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            scaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;

            scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 4f);
            float sp = 1f + Mathf.Sin(scorePunch * Mathf.PI) * 0.28f;
            score.rectTransform.localScale = new Vector3(sp, sp, 1f);

            perfectT += dt;
            perfect.gameObject.SetActive(perfectT < 0.9f);
            if (perfectT < 0.9f)
            {
                float pop = perfectT < 0.15f ? Mathf.Lerp(0.4f, 1.25f, perfectT / 0.15f) : Mathf.Lerp(1.25f, 1f, (perfectT - 0.15f) / 0.2f);
                perfect.rectTransform.localScale = Vector3.one * pop;
                SetAlpha(perfect, 1f - Mathf.InverseLerp(0.6f, 0.9f, perfectT));
            }

            bannerT += dt;
            banner.gameObject.SetActive(bannerT < 2.4f);
            if (bannerT < 2.4f)
                SetAlpha(banner, Mathf.Min(Mathf.InverseLerp(0f, 0.3f, bannerT), 1f - Mathf.InverseLerp(1.8f, 2.4f, bannerT)));

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f;
            tapMenu.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            overT += dt;
            tapOver.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            SetAlpha(tapOver, Mathf.InverseLerp(0.4f, 0.8f, overT));

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.T += dt;
                p.Text.rectTransform.anchoredPosition = p.Start + new Vector2(0, p.T * 160f);
                float s = p.T < 0.1f ? Mathf.Lerp(0.5f, 1.15f, p.T / 0.1f) : 1f;
                p.Text.rectTransform.localScale = Vector3.one * s;
                SetAlpha(p.Text, 1f - Mathf.InverseLerp(0.45f, 0.85f, p.T));
                if (p.T > 0.85f)
                {
                    Destroy(p.Text.gameObject);
                    popups.RemoveAt(i);
                }
            }
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        GameObject Panel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvasRect, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        Text Label(GameObject parent, string text, int size, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(GameOutline));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1400, size * 1.5f);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Normal;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = Color.white;
            t.raycastTarget = false;
            t.text = text;
            var outline = go.GetComponent<GameOutline>();
            outline.Size = Mathf.Max(3f, size / 15f);
            outline.ShadowOffset = new Vector2(0f, -Mathf.Max(4f, size / 11f));
            return t;
        }

        void Button(GameObject parent, string text, Vector2 anchor, Vector2 pos, Action onClick, float width = 150)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 130);
            var img = go.GetComponent<Image>();
            img.sprite = rounded;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.22f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var label = Label(go, text, text.Length > 1 ? 56 : 80, new Vector2(0.5f, 0.5f), Vector2.zero);
            label.rectTransform.sizeDelta = new Vector2(width, 130);
        }

        static Sprite MakeRoundedSprite()
        {
            const int size = 64, r = 28;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, size - r), cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d + 0.5f) * 255));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }
    }
}
