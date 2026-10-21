using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StackJump
{
    // All UI, built in code: menu (character + world pickers), HUD, popups and the game-over card.
    // The canvas keeps its short side at 1080 units, so layouts hold in landscape and portrait.
    public class GameUI : MonoBehaviour
    {
        public const string Title = "STACK <color=#FFD23A>HOP</color>";

        public Action OnPlay, OnPrevChar, OnNextChar, OnPrevWorld, OnNextWorld, OnRevive, OnRetry, OnHome, OnSound;

        static readonly Color Gold = new(1f, 0.85f, 0.25f);
        static readonly Color Green = Mats.Hex("46C93A");
        static readonly Color Orange = Mats.Hex("FF9F1C");
        static readonly Color Purple = Mats.Hex("7B4BFF");
        static readonly Color White = Mats.Hex("FFFFFF");

        CanvasScaler scaler;
        RectTransform canvasRect, popupLayer;
        GameObject menu, hud, over;
        RectTransform title;
        Text score, perfect, banner, bestBadge, hudBest, charName, charLock, worldName;
        Text overTitle, overScore, overBest, overNew, overUnlock;
        GameButton play, revive, retry, soundButton, worldLeft, worldRight;
        Image soundIcon;
        RectTransform card;
        float scorePunch, perfectT = 9f, bannerT = 9f, overT = 9f;

        class Popup { public Text Text; public float T; public Vector2 Start; }
        readonly List<Popup> popups = new();

        public void Init()
        {
            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasRect = (RectTransform)go.transform;
            UpdateScaler();

            popupLayer = Panel("Popups");

            // ----- HUD -----
            hud = Panel("HUD").gameObject;
            score = UIKit.Label(hud.transform, "0", 170, new Vector2(0.5f, 1f), new Vector2(0, -150));
            hudBest = UIKit.Label(hud.transform, "", 46, new Vector2(0.5f, 1f), new Vector2(0, -260), new Color(1, 1, 1, 0.85f));
            // Left of the tower, so it never covers the character; "+N" popups go on the right.
            perfect = UIKit.Label(hud.transform, "PERFECT!", 84, new Vector2(0.5f, 0.5f), new Vector2(-330, 120), Gold);

            // ----- Menu -----
            menu = Panel("Menu").gameObject;
            var titleText = UIKit.Label(menu.transform, Title, 140, new Vector2(0.5f, 1f), new Vector2(0, -180), null, TextAnchor.MiddleCenter, 1.3f);
            titleText.supportRichText = true;
            title = titleText.rectTransform;
            UIKit.Img(menu.transform, "Trophy", UIKit.IconSprite(Icon.Trophy), Gold, new Vector2(0f, 1f), new Vector2(80, -70), new Vector2(64, 64));
            bestBadge = UIKit.Label(menu.transform, "0", 58, new Vector2(0f, 1f), new Vector2(940, -72), null, TextAnchor.MiddleLeft);

            UIKit.Button(menu.transform, "", Icon.Left, White * 0.95f, new Vector2(0.5f, 0.5f), new Vector2(-270, -10), new Vector2(110, 110), () => OnPrevChar?.Invoke(), true)
                .transform.Find("Face/Icon").GetComponent<Image>().color = Mats.Hex("4A2A8A");
            UIKit.Button(menu.transform, "", Icon.Right, White * 0.95f, new Vector2(0.5f, 0.5f), new Vector2(270, -10), new Vector2(110, 110), () => OnNextChar?.Invoke(), true)
                .transform.Find("Face/Icon").GetComponent<Image>().color = Mats.Hex("4A2A8A");
            charName = UIKit.Label(menu.transform, "", 60, new Vector2(0.5f, 0.5f), new Vector2(0, -225));
            charLock = UIKit.Label(menu.transform, "", 40, new Vector2(0.5f, 0.5f), new Vector2(0, -280), Gold);

            // World picker sits under the title.
            UIKit.Img(menu.transform, "WorldChip", UIKit.Rounded, new Color(0.08f, 0.03f, 0.18f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0, -315), new Vector2(520, 84), true);
            worldLeft = UIKit.Button(menu.transform, "", Icon.Left, Purple, new Vector2(0.5f, 1f), new Vector2(-300, -315), new Vector2(84, 84), () => OnPrevWorld?.Invoke(), true);
            worldRight = UIKit.Button(menu.transform, "", Icon.Right, Purple, new Vector2(0.5f, 1f), new Vector2(300, -315), new Vector2(84, 84), () => OnNextWorld?.Invoke(), true);
            worldName = UIKit.Label(menu.transform, "", 48, new Vector2(0.5f, 1f), new Vector2(0, -313));
            play = UIKit.Button(menu.transform, "PLAY", Icon.Play, Green, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(460, 150), () => OnPlay?.Invoke());
            play.Pulse = true;

            // ----- Game over -----
            over = Panel("GameOver").gameObject;
            var dim = UIKit.Img(over.transform, "Dim", null, new Color(0.04f, 0.01f, 0.1f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.sizeDelta = Vector2.zero;
            card = UIKit.Img(over.transform, "Card", UIKit.Rounded, new Color(0.12f, 0.06f, 0.26f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(720, 520), true).rectTransform;
            UIKit.Img(card, "CardEdge", UIKit.Rounded, new Color(1f, 1f, 1f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(680, 100), true);
            overTitle = UIKit.Label(card, "GAME OVER", 84, new Vector2(0.5f, 1f), new Vector2(0, -62));
            UIKit.Label(card, "SCORE", 44, new Vector2(0.5f, 1f), new Vector2(0, -160), new Color(1, 1, 1, 0.7f));
            overScore = UIKit.Label(card, "0", 190, new Vector2(0.5f, 1f), new Vector2(0, -275));
            UIKit.Img(card, "Trophy", UIKit.IconSprite(Icon.Trophy), Gold, new Vector2(0.5f, 0f), new Vector2(-120, 70), new Vector2(56, 56));
            overBest = UIKit.Label(card, "BEST 0", 56, new Vector2(0.5f, 0f), new Vector2(30, 70), Gold);
            overNew = UIKit.Label(card, "NEW BEST!", 70, new Vector2(1f, 1f), new Vector2(-40, 10), Gold);
            overNew.rectTransform.localRotation = Quaternion.Euler(0, 0, -12f);
            overUnlock = UIKit.Label(over.transform, "", 46, new Vector2(0.5f, 0.5f), new Vector2(0, -150), Gold);

            revive = UIKit.Button(over.transform, "REVIVE", Icon.Video, Orange, new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(460, 130), () => OnRevive?.Invoke());
            retry = UIKit.Button(over.transform, "PLAY AGAIN", Icon.Retry, Green, new Vector2(0.5f, 0.5f), new Vector2(0, -410), new Vector2(460, 130), () => OnRetry?.Invoke());
            UIKit.Button(over.transform, "", Icon.Home, Purple, new Vector2(0.5f, 0.5f), new Vector2(-330, -410), new Vector2(110, 110), () => OnHome?.Invoke(), true);

            // ----- Always on -----
            banner = UIKit.Label(canvasRect, "", 76, new Vector2(0.5f, 0.5f), new Vector2(0, 360));
            soundButton = UIKit.Button(canvasRect, "", Icon.Sound, Purple, new Vector2(1f, 1f), new Vector2(-80, -80), new Vector2(96, 96), () => OnSound?.Invoke(), true);
            soundIcon = soundButton.transform.Find("Face/Icon").GetComponent<Image>();

            ShowMenu(new MenuInfo());
        }

        public struct MenuInfo
        {
            public int Best;
            public string CharName, CharLock, WorldName;
            public bool CharLocked, WorldArrows;
        }

        public void ShowMenu(MenuInfo info)
        {
            menu.SetActive(true);
            hud.SetActive(false);
            over.SetActive(false);
            bestBadge.text = info.Best.ToString();
            charName.text = info.CharName;
            charLock.text = info.CharLock;
            worldName.text = info.WorldName;
            worldLeft.gameObject.SetActive(info.WorldArrows);
            worldRight.gameObject.SetActive(info.WorldArrows);
            play.Text.text = info.CharLocked ? "LOCKED" : "PLAY";
        }

        public void ShowHud(int best)
        {
            menu.SetActive(false);
            over.SetActive(false);
            hud.SetActive(true);
            perfectT = 9f;
            hudBest.text = best > 0 ? "BEST " + best : "";
            SetScore(0, false);
        }

        public void ShowGameOver(int finalScore, int bestScore, bool newBest, string unlock, bool canRevive)
        {
            hud.SetActive(false);
            over.SetActive(true);
            overTitle.text = newBest ? "AMAZING!" : finalScore >= bestScore * 0.8f && finalScore > 3 ? "SO CLOSE!" : "GAME OVER";
            overScore.text = finalScore.ToString();
            overBest.text = "BEST " + bestScore;
            overNew.gameObject.SetActive(newBest);
            overUnlock.text = unlock;
            revive.gameObject.SetActive(canRevive);
            revive.Pulse = canRevive;
            retry.Pulse = !canRevive;
            overT = 0f;
        }

        public void SetMuted(bool muted) => soundIcon.sprite = UIKit.IconSprite(muted ? Icon.Mute : Icon.Sound);

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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screen, null, out var local);
            var t = UIKit.Label(popupLayer, text, 100, new Vector2(0.5f, 0.5f), local, color);
            popups.Add(new Popup { Text = t, T = 0f, Start = local });
        }

        void UpdateScaler()
        {
            bool landscape = Screen.width >= Screen.height;
            scaler.referenceResolution = landscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = landscape ? 1f : 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateScaler();

            if (menu.activeSelf)
            {
                title.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.6f) * 2.5f);
                float s = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.025f;
                title.localScale = new Vector3(s, s, 1f);
            }

            scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 4f);
            float sp = 1f + Mathf.Sin(scorePunch * Mathf.PI) * 0.3f;
            score.rectTransform.localScale = new Vector3(sp, sp, 1f);

            perfectT += dt;
            perfect.gameObject.SetActive(perfectT < 0.9f);
            if (perfectT < 0.9f)
            {
                float pop = perfectT < 0.12f ? Mathf.Lerp(0.3f, 1.3f, perfectT / 0.12f) : Mathf.Lerp(1.3f, 1f, (perfectT - 0.12f) / 0.18f);
                perfect.rectTransform.localScale = Vector3.one * pop;
                perfect.rectTransform.localRotation = Quaternion.Euler(0, 0, 8f + Mathf.Sin(perfectT * 30f) * 4f * (1f - perfectT));
                SetAlpha(perfect, 1f - Mathf.InverseLerp(0.6f, 0.9f, perfectT));
            }

            bannerT += dt;
            banner.gameObject.SetActive(bannerT < 2.4f);
            if (bannerT < 2.4f)
            {
                SetAlpha(banner, Mathf.Min(Mathf.InverseLerp(0f, 0.25f, bannerT), 1f - Mathf.InverseLerp(1.8f, 2.4f, bannerT)));
                float bs = bannerT < 0.25f ? Mathf.Lerp(1.4f, 1f, bannerT / 0.25f) : 1f;
                banner.rectTransform.localScale = Vector3.one * bs;
            }

            overT += dt;
            if (over.activeSelf)
            {
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(overT / 0.35f), 3f);
                card.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, e) * (1f + Mathf.Sin(Mathf.Clamp01(overT / 0.5f) * Mathf.PI) * 0.04f);
                overNew.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f);
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.T += dt;
                p.Text.rectTransform.anchoredPosition = p.Start + new Vector2(0, p.T * 170f);
                float s = p.T < 0.1f ? Mathf.Lerp(0.5f, 1.2f, p.T / 0.1f) : Mathf.Lerp(1.2f, 1f, (p.T - 0.1f) / 0.2f);
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

        RectTransform Panel(string name)
        {
            var rt = UIKit.Rect(canvasRect, name, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
    }
}
