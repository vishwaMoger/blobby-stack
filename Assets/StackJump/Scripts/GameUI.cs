using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StackJump
{
    // All UI, built in code: menu (character shop, world picker, modes), HUDs for solo and
    // 2-player, popups, the game-over card, the 2-player result and the daily reward calendar.
    // The canvas keeps its short side at 1080 units, so layouts hold in landscape and portrait.
    public class GameUI : MonoBehaviour
    {
        public const string Title = "BLOBBY <color=#FFD23A>STACK</color>";

        public Action OnPlay, OnDaily, OnVersus, OnPrevChar, OnNextChar, OnPrevWorld, OnNextWorld;
        public Action OnRetry, OnDoubleCoins, OnHome, OnSound, OnGift, OnRematch;
        public Action<bool> OnClaim, OnRevive; // OnRevive(true) = pay with coins

        public static readonly Color Gold = new(1f, 0.85f, 0.25f);
        public static readonly Color P1Color = Mats.Hex("FF6FA0");
        public static readonly Color P2Color = Mats.Hex("5FC8FF");
        static readonly Color Green = Mats.Hex("46C93A");
        static readonly Color Orange = Mats.Hex("FF9F1C");
        static readonly Color Purple = Mats.Hex("7B4BFF");
        static readonly Color Blue = Mats.Hex("2F8FEA");
        static readonly Color Grey = Mats.Hex("8C86A0");
        static readonly Color IconDark = Mats.Hex("4A2A8A");

        CanvasScaler scaler;
        RectTransform canvasRect, popupLayer;
        GameObject menu, hud, versusHud, over, versusOver, rewardModal;
        RectTransform title;
        bool? landscape;

        // Menu
        Text bestBadge, coinBadge, charName, charLock, worldName, dailyBadge;
        GameButton play, daily, versus, worldLeft, worldRight, gift, soundButton;
        Image giftDot;

        // HUD
        Text score, perfect, banner, hudBest, hudCoins, hudGoal;
        Text p1Score, p2Score, p1Hint, p2Hint;
        float hintT;

        // Game over
        RectTransform card;
        Text overTitle, overScore, overBest, overNew, overUnlock, overCoins;
        GameButton reviveAd, reviveCoins, retry, doubleCoins;
        Text tutorial, shotTitle;

        // Versus result
        RectTransform versusCard;
        Text versusTitle, versusScores;

        // Daily reward
        readonly List<Image> rewardTiles = new();
        readonly List<Image> rewardChecks = new();
        readonly List<Text> rewardAmounts = new();
        readonly List<Image> rewardCoins = new();
        GameButton claim, claimDouble;

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

            popupLayer = Panel("Popups");
            BuildHud();
            BuildVersusHud();
            BuildMenu();
            BuildGameOver();
            BuildVersusOver();
            BuildRewardModal();

            // Between the score block at the top and the character in the middle.
            banner = UIKit.Label(canvasRect, "", 76, new Vector2(0.5f, 0.5f), new Vector2(0, 170));
            soundButton = UIKit.Button(canvasRect, "", Icon.Sound, Purple, new Vector2(1f, 1f), new Vector2(-80, -80), new Vector2(96, 96), () => OnSound?.Invoke(), true);
            tutorial = UIKit.Label(canvasRect, "TAP OR PRESS SPACE\nTO JUMP OVER THE BLOCKS!", 60, new Vector2(0.5f, 0f), new Vector2(0, 230), Color.white);
            tutorial.rectTransform.sizeDelta = new Vector2(1400, 200);
            tutorial.gameObject.SetActive(false);
            shotTitle = UIKit.Label(canvasRect, Title, 190, new Vector2(0.5f, 1f), new Vector2(0, -180), null, TextAnchor.MiddleCenter, 1.4f);
            shotTitle.supportRichText = true;
            shotTitle.gameObject.SetActive(false);

            UpdateLayout();
            ShowMenu(new MenuInfo());
        }

        // ---------- Construction ----------

        void BuildHud()
        {
            hud = Panel("HUD").gameObject;
            score = UIKit.Label(hud.transform, "0", 170, new Vector2(0.5f, 1f), new Vector2(0, -150));
            hudBest = UIKit.Label(hud.transform, "", 46, new Vector2(0.5f, 1f), new Vector2(0, -260), new Color(1, 1, 1, 0.85f));
            hudGoal = UIKit.Label(hud.transform, "", 46, new Vector2(0.5f, 1f), new Vector2(0, -320), Gold);
            UIKit.Img(hud.transform, "Coin", UIKit.IconSprite(Icon.Coin), Gold, new Vector2(0f, 1f), new Vector2(80, -80), new Vector2(64, 64));
            hudCoins = UIKit.Label(hud.transform, "0", 58, new Vector2(0f, 1f), new Vector2(940, -82), null, TextAnchor.MiddleLeft);
            // Left of the tower, so it never covers the character; "+N" popups go on the right.
            perfect = UIKit.Label(hud.transform, "PERFECT!", 84, new Vector2(0.5f, 0.5f), new Vector2(-330, 120), Gold);
        }

        void BuildVersusHud()
        {
            versusHud = Panel("VersusHUD").gameObject;
            var divider = UIKit.Img(versusHud.transform, "Divider", null, new Color(0.05f, 0.02f, 0.12f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 4000));
            divider.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            divider.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            divider.rectTransform.sizeDelta = new Vector2(8, 0);
            UIKit.Label(versusHud.transform, "P1", 54, new Vector2(0.25f, 1f), new Vector2(0, -60), P1Color);
            UIKit.Label(versusHud.transform, "P2", 54, new Vector2(0.75f, 1f), new Vector2(0, -60), P2Color);
            p1Score = UIKit.Label(versusHud.transform, "0", 140, new Vector2(0.25f, 1f), new Vector2(0, -160));
            p2Score = UIKit.Label(versusHud.transform, "0", 140, new Vector2(0.75f, 1f), new Vector2(0, -160));
            p1Hint = UIKit.Label(versusHud.transform, "PRESS W\nOR TAP HERE", 52, new Vector2(0.25f, 0f), new Vector2(0, 200), P1Color);
            p2Hint = UIKit.Label(versusHud.transform, "PRESS UP ARROW\nOR TAP HERE", 52, new Vector2(0.75f, 0f), new Vector2(0, 200), P2Color);
            p1Hint.rectTransform.sizeDelta = p2Hint.rectTransform.sizeDelta = new Vector2(900, 180);
        }

        void BuildMenu()
        {
            menu = Panel("Menu").gameObject;
            var titleText = UIKit.Label(menu.transform, Title, 140, new Vector2(0.5f, 1f), new Vector2(0, -180), null, TextAnchor.MiddleCenter, 1.3f);
            titleText.supportRichText = true;
            title = titleText.rectTransform;

            UIKit.Img(menu.transform, "Trophy", UIKit.IconSprite(Icon.Trophy), Gold, new Vector2(0f, 1f), new Vector2(80, -70), new Vector2(60, 60));
            bestBadge = UIKit.Label(menu.transform, "0", 54, new Vector2(0f, 1f), new Vector2(935, -72), null, TextAnchor.MiddleLeft);
            UIKit.Img(menu.transform, "Coin", UIKit.IconSprite(Icon.Coin), Gold, new Vector2(0f, 1f), new Vector2(80, -150), new Vector2(60, 60));
            coinBadge = UIKit.Label(menu.transform, "0", 54, new Vector2(0f, 1f), new Vector2(935, -152), Gold, TextAnchor.MiddleLeft);

            gift = UIKit.Button(menu.transform, "", Icon.Gift, Orange, new Vector2(1f, 1f), new Vector2(-200, -80), new Vector2(96, 96), () => OnGift?.Invoke(), true);
            giftDot = UIKit.Img(gift.transform, "Dot", UIKit.Circle, Mats.Hex("FF3B3B"), new Vector2(1f, 1f), new Vector2(-12, -12), new Vector2(30, 30));

            var left = UIKit.Button(menu.transform, "", Icon.Left, Color.white * 0.95f, new Vector2(0.5f, 0.5f), new Vector2(-270, -10), new Vector2(110, 110), () => OnPrevChar?.Invoke(), true);
            left.SetIcon(Icon.Left, IconDark);
            var right = UIKit.Button(menu.transform, "", Icon.Right, Color.white * 0.95f, new Vector2(0.5f, 0.5f), new Vector2(270, -10), new Vector2(110, 110), () => OnNextChar?.Invoke(), true);
            right.SetIcon(Icon.Right, IconDark);
            charName = UIKit.Label(menu.transform, "", 60, new Vector2(0.5f, 0.5f), new Vector2(0, -225));
            charLock = UIKit.Label(menu.transform, "", 40, new Vector2(0.5f, 0.5f), new Vector2(0, -280), Gold);

            UIKit.Img(menu.transform, "WorldChip", UIKit.Rounded, new Color(0.08f, 0.03f, 0.18f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0, -315), new Vector2(520, 84), true);
            worldLeft = UIKit.Button(menu.transform, "", Icon.Left, Purple, new Vector2(0.5f, 1f), new Vector2(-300, -315), new Vector2(84, 84), () => OnPrevWorld?.Invoke(), true);
            worldRight = UIKit.Button(menu.transform, "", Icon.Right, Purple, new Vector2(0.5f, 1f), new Vector2(300, -315), new Vector2(84, 84), () => OnNextWorld?.Invoke(), true);
            worldName = UIKit.Label(menu.transform, "", 48, new Vector2(0.5f, 1f), new Vector2(0, -313));

            play = UIKit.Button(menu.transform, "PLAY", Icon.Play, Green, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(460, 150), () => OnPlay?.Invoke());
            play.Pulse = true;
            daily = UIKit.Button(menu.transform, "DAILY", Icon.Trophy, Purple, new Vector2(0.5f, 0f), new Vector2(-480, 150), new Vector2(380, 120), () => OnDaily?.Invoke());
            dailyBadge = UIKit.Label(daily.transform, "", 42, new Vector2(0.5f, 1f), new Vector2(0, 26), Gold);
            versus = UIKit.Button(menu.transform, "2 PLAYER", Icon.Users, Blue, new Vector2(0.5f, 0f), new Vector2(480, 150), new Vector2(380, 120), () => OnVersus?.Invoke());
        }

        void BuildGameOver()
        {
            over = Panel("GameOver").gameObject;
            Dim(over.transform);
            card = UIKit.Img(over.transform, "Card", UIKit.Rounded, new Color(0.12f, 0.06f, 0.26f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(720, 560), true).rectTransform;
            UIKit.Img(card, "CardEdge", UIKit.Rounded, new Color(1f, 1f, 1f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(680, 100), true);
            overTitle = UIKit.Label(card, "GAME OVER", 80, new Vector2(0.5f, 1f), new Vector2(0, -62));
            UIKit.Label(card, "SCORE", 42, new Vector2(0.5f, 1f), new Vector2(0, -155), new Color(1, 1, 1, 0.7f));
            overScore = UIKit.Label(card, "0", 180, new Vector2(0.5f, 1f), new Vector2(0, -262));
            UIKit.Img(card, "Trophy", UIKit.IconSprite(Icon.Trophy), Gold, new Vector2(0.5f, 0f), new Vector2(-230, 150), new Vector2(50, 50));
            overBest = UIKit.Label(card, "BEST 0", 50, new Vector2(0.5f, 0f), new Vector2(-170, 150), Gold, TextAnchor.MiddleLeft);
            overBest.rectTransform.sizeDelta = new Vector2(600, 80);
            overBest.rectTransform.pivot = new Vector2(0f, 0.5f);
            UIKit.Img(card, "Coin", UIKit.IconSprite(Icon.Coin), Gold, new Vector2(0.5f, 0f), new Vector2(-230, 70), new Vector2(50, 50));
            overCoins = UIKit.Label(card, "+0", 54, new Vector2(0.5f, 0f), new Vector2(-170, 70), Gold, TextAnchor.MiddleLeft);
            overCoins.rectTransform.sizeDelta = new Vector2(400, 80);
            overCoins.rectTransform.pivot = new Vector2(0f, 0.5f);
            doubleCoins = UIKit.Button(card, "x2", Icon.Video, Orange, new Vector2(0.5f, 0f), new Vector2(200, 72), new Vector2(200, 90), () => OnDoubleCoins?.Invoke());
            overNew = UIKit.Label(card, "NEW BEST!", 66, new Vector2(1f, 1f), new Vector2(-40, 10), Gold);
            overNew.rectTransform.localRotation = Quaternion.Euler(0, 0, -12f);
            overUnlock = UIKit.Label(over.transform, "", 44, new Vector2(0.5f, 0.5f), new Vector2(0, -150), Gold);

            // Revive offers use the same size, font and color as PLAY AGAIN (CrazyGames ad rules:
            // the skip option must match the reward button).
            reviveAd = UIKit.Button(over.transform, "REVIVE", Icon.Video, Green, new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(460, 125), () => OnRevive?.Invoke(false));
            reviveCoins = UIKit.Button(over.transform, "REVIVE 100", Icon.Coin, Green, new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(460, 125), () => OnRevive?.Invoke(true));
            retry = UIKit.Button(over.transform, "PLAY AGAIN", Icon.Retry, Green, new Vector2(0.5f, 0.5f), new Vector2(0, -405), new Vector2(460, 125), () => OnRetry?.Invoke());
            UIKit.Button(over.transform, "", Icon.Home, Purple, new Vector2(0.5f, 0.5f), new Vector2(-330, -405), new Vector2(110, 110), () => OnHome?.Invoke(), true);
        }

        void BuildVersusOver()
        {
            versusOver = Panel("VersusOver").gameObject;
            Dim(versusOver.transform);
            versusCard = UIKit.Img(versusOver.transform, "Card", UIKit.Rounded, new Color(0.12f, 0.06f, 0.26f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(760, 400), true).rectTransform;
            versusTitle = UIKit.Label(versusCard, "PLAYER 1 WINS!", 86, new Vector2(0.5f, 1f), new Vector2(0, -90));
            versusScores = UIKit.Label(versusCard, "0 - 0", 130, new Vector2(0.5f, 0f), new Vector2(0, 120));
            versusScores.supportRichText = true;
            UIKit.Button(versusOver.transform, "REMATCH", Icon.Retry, Green, new Vector2(0.5f, 0.5f), new Vector2(40, -230), new Vector2(460, 125), () => OnRematch?.Invoke());
            UIKit.Button(versusOver.transform, "", Icon.Home, Purple, new Vector2(0.5f, 0.5f), new Vector2(-290, -230), new Vector2(110, 110), () => OnHome?.Invoke(), true);
        }

        void BuildRewardModal()
        {
            rewardModal = Panel("DailyReward").gameObject;
            var dim = Dim(rewardModal.transform);
            dim.color = new Color(0.04f, 0.01f, 0.1f, 0.7f);
            dim.raycastTarget = true; // block clicks to the menu underneath
            var c = UIKit.Img(rewardModal.transform, "Card", UIKit.Rounded, new Color(0.12f, 0.06f, 0.26f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1020, 520), true).rectTransform;
            UIKit.Img(c, "Gift", UIKit.IconSprite(Icon.Gift), Orange, new Vector2(0.5f, 1f), new Vector2(-300, -70), new Vector2(70, 70));
            UIKit.Label(c, "DAILY REWARD", 76, new Vector2(0.5f, 1f), new Vector2(30, -70));
            UIKit.Label(c, "COME BACK EVERY DAY FOR BIGGER REWARDS!", 38, new Vector2(0.5f, 1f), new Vector2(0, -138), new Color(1, 1, 1, 0.75f));
            for (int i = 0; i < Daily.Rewards.Length; i++)
            {
                float x = (i - 3) * 136f;
                var tile = UIKit.Img(c, "Tile", UIKit.Rounded, Color.white, new Vector2(0.5f, 0.5f), new Vector2(x, -30), new Vector2(124, 190), true);
                UIKit.Label(tile.transform, "DAY " + (i + 1), 34, new Vector2(0.5f, 1f), new Vector2(0, -26));
                rewardCoins.Add(UIKit.Img(tile.transform, "Coin", UIKit.IconSprite(Icon.Coin), Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(54, 54)));
                var amount = UIKit.Label(tile.transform, Daily.Rewards[i].ToString(), 38, new Vector2(0.5f, 0f), new Vector2(0, 32));
                var check = UIKit.Img(tile.transform, "Check", UIKit.IconSprite(Icon.Check), Green, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(80, 80));
                rewardTiles.Add(tile);
                rewardAmounts.Add(amount);
                rewardChecks.Add(check);
            }
            claim = UIKit.Button(c, "CLAIM", Icon.Coin, Green, new Vector2(0.5f, 0f), new Vector2(-150, 30), new Vector2(360, 120), () => OnClaim?.Invoke(false));
            claimDouble = UIKit.Button(c, "x2", Icon.Video, Orange, new Vector2(0.5f, 0f), new Vector2(190, 30), new Vector2(260, 120), () => OnClaim?.Invoke(true));
            claim.Pulse = true;
            rewardModal.SetActive(false);
        }

        Image Dim(Transform parent)
        {
            var dim = UIKit.Img(parent, "Dim", null, new Color(0.04f, 0.01f, 0.1f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.sizeDelta = Vector2.zero;
            return dim;
        }

        // ---------- Screens ----------

        public struct MenuInfo
        {
            public int Best, Coins, Price;
            public string CharName, CharLock, WorldName, DailyBadge;
            public bool CharOwned, CanAfford, WorldArrows, GiftReady;
        }

        public void ShowMenu(MenuInfo info)
        {
            HideAll();
            menu.SetActive(true);
            bestBadge.text = info.Best.ToString();
            coinBadge.text = info.Coins.ToString();
            charName.text = info.CharName;
            charLock.text = info.CharLock;
            worldName.text = info.WorldName;
            worldLeft.gameObject.SetActive(info.WorldArrows);
            worldRight.gameObject.SetActive(info.WorldArrows);
            dailyBadge.text = info.DailyBadge;
            giftDot.gameObject.SetActive(info.GiftReady);
            if (info.CharOwned)
            {
                play.Text.text = "PLAY";
                play.SetIcon(Icon.Play, Color.white);
                play.SetColor(Green);
            }
            else
            {
                play.Text.text = info.Price.ToString();
                play.SetIcon(Icon.Coin, Gold);
                play.SetColor(info.CanAfford ? Orange : Grey);
            }
            play.Pulse = info.CharOwned || info.CanAfford;
        }

        public void SetCoins(int coins) => coinBadge.text = coins.ToString();

        public void ShowHud(int best, int dailyGoal)
        {
            HideAll();
            hud.SetActive(true);
            perfectT = 9f;
            hudBest.text = best > 0 ? "BEST " + best : "";
            hudGoal.text = dailyGoal > 0 ? "GOAL " + dailyGoal : "";
            SetScore(0, false);
            SetRunCoins(0);
        }

        public void SetRunCoins(int coins) => hudCoins.text = coins.ToString();

        public void ShowVersusHud()
        {
            HideAll();
            versusHud.SetActive(true);
            SetVersusScore(0, 0);
            SetVersusScore(1, 0);
            hintT = 0f;
        }

        public void SetVersusScore(int player, int value) => (player == 0 ? p1Score : p2Score).text = value.ToString();

        public struct GameOverInfo
        {
            public int Score, Best, Coins, DailyGoal, DailyBest, ReviveCost;
            public bool NewBest, CanReviveAd, CanReviveCoins, CanDouble, Daily, DailyJustDone;
            public string Unlock;
        }

        public void ShowGameOver(GameOverInfo info)
        {
            HideAll();
            over.SetActive(true);
            if (info.Daily)
            {
                overTitle.text = info.DailyJustDone ? "CHALLENGE DONE!" : "DAILY TOWER";
                overBest.text = "TODAY " + info.DailyBest + " / GOAL " + info.DailyGoal;
                overNew.gameObject.SetActive(false);
                overUnlock.text = info.DailyJustDone ? "+" + Daily.ChallengeReward + " BONUS COINS!" : "";
            }
            else
            {
                overTitle.text = info.NewBest ? "AMAZING!" : info.Score >= info.Best * 0.8f && info.Score > 3 ? "SO CLOSE!" : "GAME OVER";
                overBest.text = "BEST " + info.Best;
                overNew.gameObject.SetActive(info.NewBest);
                overUnlock.text = info.Unlock;
            }
            overScore.text = info.Score.ToString();
            overCoins.text = "+" + info.Coins;
            doubleCoins.gameObject.SetActive(info.CanDouble);
            reviveAd.gameObject.SetActive(info.CanReviveAd);
            reviveCoins.gameObject.SetActive(info.CanReviveCoins);
            reviveCoins.Text.text = "REVIVE " + info.ReviveCost;
            reviveCoins.SetIcon(Icon.Coin, Gold);
            // Side by side when both revive options are offered.
            bool both = info.CanReviveAd && info.CanReviveCoins;
            SetButtonX(reviveAd, both ? -240f : 0f);
            SetButtonX(reviveCoins, both ? 240f : 0f);
            retry.Pulse = !info.CanReviveAd && !info.CanReviveCoins;
            overT = 0f;
        }

        static void SetButtonX(GameButton b, float x)
        {
            var rt = (RectTransform)b.transform;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }

        public void HideRewardedButtons()
        {
            reviveAd.gameObject.SetActive(false);
            doubleCoins.gameObject.SetActive(false);
            if (reviveCoins.gameObject.activeSelf) SetButtonX(reviveCoins, 0f);
        }

        // First run: a pulsing hint until the player clears a block.
        public void ShowTutorial(bool show) => tutorial.gameObject.SetActive(show);

        // Cover-image mode: no UI except a big title.
        public void ShowShot(bool portrait)
        {
            HideAll();
            soundButton.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            shotTitle.gameObject.SetActive(true);
            shotTitle.fontSize = portrait ? 150 : 190;
            shotTitle.rectTransform.anchoredPosition = new Vector2(0, portrait ? -230 : -180);
            shotTitle.rectTransform.sizeDelta = new Vector2(1800, shotTitle.fontSize * 1.5f);
        }

        public void SetDoubledCoins(int coins)
        {
            overCoins.text = "+" + coins;
            doubleCoins.gameObject.SetActive(false);
        }

        public void ShowVersusResult(int winner, int s1, int s2)
        {
            HideAll();
            versusOver.SetActive(true);
            versusTitle.text = winner < 0 ? "IT'S A DRAW!" : "PLAYER " + (winner + 1) + " WINS!";
            versusTitle.color = winner == 0 ? P1Color : winner == 1 ? P2Color : Color.white;
            versusScores.text = $"<color=#{ColorUtility.ToHtmlStringRGB(P1Color)}>{s1}</color>  -  <color=#{ColorUtility.ToHtmlStringRGB(P2Color)}>{s2}</color>";
            overT = 0f;
        }

        public void ShowDailyReward(int index, bool canDouble)
        {
            rewardModal.SetActive(true);
            for (int i = 0; i < rewardTiles.Count; i++)
            {
                bool claimed = i < index;
                bool today = i == index;
                rewardTiles[i].color = today ? Gold : claimed ? new Color(0.3f, 0.2f, 0.5f, 0.9f) : new Color(1f, 1f, 1f, 0.16f);
                rewardChecks[i].gameObject.SetActive(claimed);
                rewardAmounts[i].color = today ? Mats.Hex("4A2A00") : Color.white;
                rewardCoins[i].color = today ? Mats.Hex("C26A00") : Gold;
                rewardCoins[i].gameObject.SetActive(!claimed);
                rewardTiles[i].rectTransform.localScale = Vector3.one * (today ? 1.1f : 1f);
            }
            claimDouble.gameObject.SetActive(canDouble);
            overT = 0f;
        }

        public void HideDailyReward() => rewardModal.SetActive(false);
        public bool RewardOpen => rewardModal.activeSelf;

        void HideAll()
        {
            menu.SetActive(false);
            hud.SetActive(false);
            versusHud.SetActive(false);
            over.SetActive(false);
            versusOver.SetActive(false);
        }

        // Preview-video recording: hide the always-on sound button.
        public void SetCleanCapture() => soundButton.gameObject.SetActive(false);

        public void SetMuted(bool muted) => soundButton.SetIcon(muted ? Icon.Mute : Icon.Sound, Color.white);

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

        public void Banner(string text, int size = 76)
        {
            banner.text = text;
            banner.fontSize = size;
            banner.rectTransform.sizeDelta = new Vector2(1600, size * 1.5f);
            bannerT = 0f;
        }

        public void PopupAt(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screen, null, out var local);
            var t = UIKit.Label(popupLayer, text, 90, new Vector2(0.5f, 0.5f), local, color);
            popups.Add(new Popup { Text = t, T = 0f, Start = local });
        }

        // ---------- Layout ----------

        void UpdateLayout()
        {
            bool isLandscape = Screen.width >= Screen.height;
            if (landscape == isLandscape) return;
            landscape = isLandscape;
            scaler.referenceResolution = isLandscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = isLandscape ? 1f : 0f;

            // Mode buttons flank PLAY in landscape and sit above it in portrait.
            ((RectTransform)daily.transform).anchoredPosition = isLandscape ? new Vector2(-480, 150) : new Vector2(-215, 330);
            ((RectTransform)versus.transform).anchoredPosition = isLandscape ? new Vector2(480, 150) : new Vector2(215, 330);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateLayout();

            if (menu.activeSelf)
            {
                title.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.6f) * 2.5f);
                float s = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.025f;
                title.localScale = new Vector3(s, s, 1f);
                giftDot.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f);
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
                banner.rectTransform.localScale = Vector3.one * (bannerT < 0.25f ? Mathf.Lerp(1.4f, 1f, bannerT / 0.25f) : 1f);
            }

            if (tutorial.gameObject.activeSelf)
            {
                float tp = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f;
                tutorial.rectTransform.localScale = new Vector3(tp, tp, 1f);
            }

            if (versusHud.activeSelf)
            {
                hintT += dt;
                float a = 1f - Mathf.InverseLerp(3f, 3.6f, hintT);
                SetAlpha(p1Hint, a);
                SetAlpha(p2Hint, a);
            }

            overT += dt;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(overT / 0.35f), 3f);
            float pulse = Mathf.Lerp(0.6f, 1f, e) * (1f + Mathf.Sin(Mathf.Clamp01(overT / 0.5f) * Mathf.PI) * 0.04f);
            if (over.activeSelf)
            {
                card.localScale = Vector3.one * pulse;
                overNew.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f);
            }
            if (versusOver.activeSelf) versusCard.localScale = Vector3.one * pulse;

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
