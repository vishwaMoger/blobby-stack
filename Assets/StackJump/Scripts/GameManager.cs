using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace StackJump
{
    // Runs the game modes on top of Lanes:
    //  Classic  - endless climb; worlds change every 20 blocks; one rewarded revive per run.
    //  Daily    - today's seeded tower (same for everyone), with a goal and a once-a-day bonus.
    //  Versus   - local 2-player race on a split screen; last one standing (or higher) wins.
    // Also handles coins, the character shop and the daily login reward.
    [DefaultExecutionOrder(100)]
    public class GameManager : MonoBehaviour
    {
        enum State { Boot, Menu, Playing, Ending, Result, Busy }
        enum Mode { Classic, Daily, Versus }

        const int BlocksPerWorld = 20;
        const float VersusOffset = 2.6f;
        const float CountdownTime = 3f;
        int countdownShown;

        Camera cam, cam2;
        CameraRig rig, rig2;
        Scenery scenery;
        GameUI ui;
        Fx fx;
        Sfx sfx;
        Lane solo, p1, p2;

        const int ReviveCost = 100;

        State state = State.Boot;
        Mode mode;
        int charIndex, world, startWorld, runs, runCoins, lastAdReviveRun = -99;
        bool revived, dailyJustDone, tutorial, rewardPrompted;
        bool rewardedOffered, rewardedWatched; // on the current game-over screen
        float stateTime;

        // Test hooks: ?autoplay&soak&world=N&mode=daily|versus in the page URL.
        bool autoplay, soak, trailer;
        int forcedWorld = -1;
        string forcedMode = "", shot = "";

        WorldTheme Theme => Themes.All[world];
        CharacterDef Char => Characters.All[charIndex];

        void Awake()
        {
            Application.targetFrameRate = -1;
            // After a hitch (tab switch, slow device) step at most 1/15 s, so blocks and jumps
            // slow down instead of teleporting through each other.
            Time.maximumDeltaTime = 1f / 15f;
            cam = Camera.main;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            scenery = gameObject.AddComponent<Scenery>();
            scenery.Init(cam, FindFirstObjectByType<Light>());
            rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            CreateSecondCamera();

            fx = gameObject.AddComponent<Fx>();
            fx.Init();
            sfx = gameObject.AddComponent<Sfx>();
            sfx.Init();
            ui = gameObject.AddComponent<GameUI>();
            ui.Init();
            WireUI();

            solo = MakeLane("Solo", 0f, rig);
            p1 = MakeLane("P1", -VersusOffset, rig);
            p2 = MakeLane("P2", VersusOffset, rig2);

            ReadTestParams();
            world = Mathf.Max(0, forcedWorld);
            scenery.SetWorld(Theme, true);
            ShowSoloLane(Characters.All[0], false);
        }

        void Start() => Platform.Init(Boot);

        void Boot()
        {
            Platform.LoadAudioPref();
            ui.SetMuted(Platform.UserMuted);
            charIndex = Mathf.Clamp(Save.Character, 0, Characters.All.Length - 1);
            if (!Economy.Owns(charIndex)) charIndex = 0;
            startWorld = forcedWorld >= 0 ? forcedWorld : Mathf.Clamp(Save.World, 0, Mathf.Min(Save.MaxWorld, Themes.All.Length - 1));
            if (shot != "")
            {
                SetupShot();
                return;
            }
#if UNITY_EDITOR
            if (trailer)
            {
                EnterMenu();
                var director = gameObject.AddComponent<TrailerDirector>();
                director.Game = this;
                director.Cam = cam;
                director.Cam2 = cam2;
                director.UI = ui;
                return;
            }
#endif
            // CrazyGames Full Launch: new players land directly in gameplay. Returning players get
            // the menu; the daily reward pops up once they come back to it after a run.
            bool firstTime = Save.Best == 0 && Save.Coins == 0 && Save.RewardDay == 0;
            EnterMenu();
            if (firstTime && !autoplay)
            {
                StartRun(Mode.Classic);
                tutorial = true;
                ui.ShowTutorial(true);
            }
        }

        void CreateSecondCamera()
        {
            var go = new GameObject("Camera P2");
            cam2 = go.AddComponent<Camera>();
            cam2.CopyFrom(cam);
            var data = cam2.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            rig2 = go.AddComponent<CameraRig>();
            scenery.AddCamera(cam2);
            cam2.enabled = false;
        }

        Lane MakeLane(string name, float x, CameraRig laneRig)
        {
            var lane = new Lane(name, x, laneRig, () => Theme);
            lane.Jumped += _ => sfx.Jump();
            lane.Landed += l =>
            {
                sfx.Land();
                fx.Dust(l.Player.transform.position, Color.Lerp(scenery.Current.Fog, Color.white, 0.5f));
            };
            lane.Passed += OnPassed;
            lane.Hit += OnHit;
            lane.Settled += OnSettled;
            lane.CoinCollected += l =>
            {
                sfx.Coin();
                ui.PopupAt(l.Rig.Cam, l.Player.transform.position + new Vector3(-1.1f, 1.2f, 0), "+" + Economy.CoinPickup, GameUI.Gold);
                fx.Sparkle(l.Player.transform.position + Vector3.up * 0.9f, GameUI.Gold, 16);
                if (l == solo) ui.SetRunCoins(l.Coins);
            };
            lane.SetVisible(false);
            return lane;
        }

        void WireUI()
        {
            ui.OnPlay = OnPlayPressed;
            ui.OnDaily = () => { sfx.Click(); StartRun(Mode.Daily); };
            ui.OnVersus = () => { sfx.Click(); StartVersus(); };
            ui.OnPrevChar = () => CycleChar(-1);
            ui.OnNextChar = () => CycleChar(1);
            ui.OnPrevWorld = () => CycleWorld(-1);
            ui.OnNextWorld = () => CycleWorld(1);
            ui.OnRevive = Revive;
            ui.OnRetry = Retry;
            ui.OnRematch = Rematch;
            ui.OnDoubleCoins = DoubleCoins;
            ui.OnHome = () => { sfx.Click(); EnterMenu(); };
            ui.OnSound = ToggleSound;
            ui.OnGift = () =>
            {
                sfx.Click();
                if (Daily.RewardAvailable) ShowDailyReward();
                else ui.Banner("COME BACK TOMORROW!");
            };
            ui.OnClaim = ClaimDailyReward;
        }

        // ---------- Lanes and cameras ----------

        void SetSplit(bool split)
        {
            cam.rect = split ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 1f, 1f);
            cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            cam2.enabled = split;
            solo.SetVisible(!split);
            p1.SetVisible(split);
            p2.SetVisible(split);
        }

        void ShowSoloLane(CharacterDef character, bool silhouette)
        {
            SetSplit(false);
            scenery.SetGround(Theme, new[] { 0f });
            solo.Player.SetCharacter(character, silhouette);
            solo.Reset(-1, 0);
        }

        int PlayableChar => Economy.Owns(charIndex) ? charIndex : Economy.Owns(Save.Character) ? Save.Character : 0;

        // ---------- Menu ----------

        void EnterMenu()
        {
            Platform.GameplayStop();
            state = State.Menu;
            stateTime = 0f;
            mode = Mode.Classic;
            if (world != startWorld)
            {
                world = startWorld;
                scenery.SetWorld(Theme, false);
            }
            ShowSoloLane(Char, !Economy.Owns(charIndex));
            RefreshMenu();
            if (runs > 0 && !rewardPrompted && Daily.RewardAvailable && !autoplay)
            {
                rewardPrompted = true;
                ShowDailyReward();
            }
        }

        void RefreshMenu()
        {
            bool owned = Economy.Owns(charIndex);
            int maxWorld = Mathf.Min(Save.MaxWorld, Themes.All.Length - 1);
            ui.ShowMenu(new GameUI.MenuInfo
            {
                Best = Save.Best,
                Coins = Save.Coins,
                Price = Char.Price,
                CharName = owned ? Char.Name : "???",
                CharLock = owned ? "" : "BUY IT, OR SCORE " + Char.Unlock,
                CharOwned = owned,
                CanAfford = Save.Coins >= Char.Price,
                WorldName = Theme.Name,
                WorldArrows = maxWorld > 0,
                DailyBadge = Daily.ChallengeDone ? "DONE!" : "GOAL " + Daily.Goal,
                GiftReady = Daily.RewardAvailable,
            });
        }

        void CycleChar(int delta)
        {
            sfx.Click();
            charIndex = (charIndex + delta + Characters.All.Length) % Characters.All.Length;
            if (Economy.Owns(charIndex)) Save.Character = charIndex;
            solo.Player.SetCharacter(Char, !Economy.Owns(charIndex));
            RefreshMenu();
        }

        void CycleWorld(int delta)
        {
            sfx.Click();
            int count = Mathf.Min(Save.MaxWorld, Themes.All.Length - 1) + 1;
            startWorld = (startWorld + delta + count) % count;
            Save.World = startWorld;
            world = startWorld;
            scenery.SetWorld(Theme, false);
            ShowSoloLane(Char, !Economy.Owns(charIndex));
            RefreshMenu();
        }

        void OnPlayPressed()
        {
            if (state != State.Menu || ui.RewardOpen) return;
            if (Economy.Owns(charIndex))
            {
                StartRun(Mode.Classic);
                return;
            }
            if (Economy.TryBuy(charIndex))
            {
                Save.Character = charIndex;
                sfx.Fanfare();
                fx.Confetti(solo.Player.transform.position + Vector3.up * 1.5f, Theme.Palette);
                ui.Banner(Char.Name + " UNLOCKED!");
                solo.Player.SetCharacter(Char, false);
                RefreshMenu();
            }
            else
            {
                sfx.Click();
                ui.Banner("NOT ENOUGH COINS");
            }
        }

        // ---------- Daily reward ----------

        void ShowDailyReward() => ui.ShowDailyReward(Daily.RewardIndex, Platform.RewardedAvailable);

        void ClaimDailyReward(bool doubled)
        {
            if (!Daily.RewardAvailable) return;
            sfx.Click();
            if (!doubled)
            {
                FinishClaim(Daily.ClaimReward(false));
                return;
            }
            state = State.Busy;
            Platform.Rewarded(ok =>
            {
                state = State.Menu;
                FinishClaim(Daily.ClaimReward(ok));
            });
        }

        void FinishClaim(int amount)
        {
            ui.HideDailyReward();
            sfx.Fanfare();
            fx.Confetti(solo.Player.transform.position + Vector3.up * 1.5f, new[] { GameUI.Gold, Color.white, Mats.Hex("FF9F1C") });
            ui.Banner("+" + amount + " COINS!");
            RefreshMenu();
        }

        // ---------- Runs ----------

        void StartRun(Mode m)
        {
            if (state != State.Menu && state != State.Result && state != State.Busy) return;
            mode = m;
            charIndex = PlayableChar;
            int seed = -1;
            if (m == Mode.Daily)
            {
                seed = Daily.Seed;
                world = Daily.World;
            }
            else world = startWorld;
            scenery.SetWorld(Theme, false);
            ShowSoloLane(Char, false);
            solo.Reset(seed, 0);
            solo.Active = true;
            revived = dailyJustDone = false;
            runCoins = 0;
            runs++;
            state = State.Playing;
            stateTime = 0f;
            ui.ShowHud(m == Mode.Daily ? Daily.ChallengeBest : Save.Best, m == Mode.Daily ? Daily.Goal : 0);
            ui.Banner(m == Mode.Daily ? "DAILY TOWER" : Theme.Name);
            Platform.GameplayStart();
        }

        void StartVersus() => StartVersus(CountdownTime);

        void StartVersus(float countdown)
        {
            if (state != State.Menu && state != State.Result && state != State.Busy) return;
            mode = Mode.Versus;
            world = startWorld;
            scenery.SetWorld(Theme, false);
            SetSplit(true);
            scenery.SetGround(Theme, new[] { -VersusOffset, VersusOffset });
            int c1 = PlayableChar;
            int c2 = (c1 + 1) % Characters.All.Length;
            p1.Player.SetCharacter(Characters.All[c1], false);
            p2.Player.SetCharacter(Characters.All[c2], false);
            // A 3-2-1 countdown gives both players time to get ready before the first blocks.
            p1.Reset(-1, -1, countdown + 0.3f);
            p2.Reset(-1, 1, countdown + 0.3f);
            p1.Active = p2.Active = true;
            runs++;
            state = State.Playing;
            stateTime = 0f;
            countdownShown = countdown > 0.5f ? -1 : 4;
            ui.ShowVersusHud();
            if (countdownShown == 4) ui.Banner("GO!", 200, 1f);
            Platform.GameplayStart();
        }

        void Retry()
        {
            if (state != State.Result) return;
            sfx.Click();
            state = State.Busy;
            var m = mode;
            // No midgame ad right after a "watch to keep playing" offer, or after a rewarded ad
            // on this screen (CrazyGames: don't pair or chain ads).
            if (runs >= 2 && !rewardedOffered && !rewardedWatched) Platform.Midgame(() => StartRun(m));
            else StartRun(m);
        }

        void Rematch()
        {
            if (state != State.Result) return;
            sfx.Click();
            state = State.Busy;
            if (runs >= 2) Platform.Midgame(StartVersus);
            else StartVersus();
        }

        void Revive(bool withCoins)
        {
            if (state != State.Result || revived || mode == Mode.Versus) return;
            sfx.Click();
            if (withCoins)
            {
                if (Save.Coins < ReviveCost) return;
                Save.Coins -= ReviveCost;
                ResumeAfterRevive();
                return;
            }
            state = State.Busy;
            Platform.Rewarded(ok =>
            {
                if (!ok)
                {
                    // No reward on an ad error; the ad buttons hide for the rest of the session.
                    state = State.Result;
                    ui.HideRewardedButtons();
                    ui.Banner("AD NOT AVAILABLE");
                    return;
                }
                lastAdReviveRun = runs;
                ResumeAfterRevive();
            });
        }

        void ResumeAfterRevive()
        {
            {
                revived = true;
                // Coins were banked at game over; count only new ones from here.
                solo.Coins = 0;
                solo.Revive();
                fx.Sparkle(solo.Player.transform.position + Vector3.up * 0.5f, Color.white, 30);
                state = State.Playing;
                stateTime = 0f;
                ui.ShowHud(mode == Mode.Daily ? Daily.ChallengeBest : Save.Best, mode == Mode.Daily ? Daily.Goal : 0);
                ui.SetScore(solo.Score, false);
                ui.SetRunCoins(0);
                ui.Banner("KEEP GOING!");
                Platform.GameplayStart();
            }
        }

        void DoubleCoins()
        {
            if (state != State.Result || runCoins <= 0) return;
            sfx.Click();
            state = State.Busy;
            Platform.Rewarded(ok =>
            {
                state = State.Result;
                if (!ok)
                {
                    ui.HideRewardedButtons();
                    ui.Banner("AD NOT AVAILABLE");
                    return;
                }
                rewardedWatched = true;
                Economy.Add(runCoins);
                runCoins *= 2;
                ui.SetDoubledCoins(runCoins);
                sfx.Coin();
            });
        }

        void EnterSoloResult()
        {
            Platform.GameplayStop();
            state = State.Result;
            stateTime = 0f;
            runCoins = solo.Coins;
            Economy.Add(runCoins);

            // Revive is a special offer, not an every-death thing: only after a good run, and the
            // ad version at most once every 3 runs. Coins are always an alternative to the ad.
            bool goodRun = !revived && solo.Score >= 10 && (mode == Mode.Daily || solo.Score >= Save.Best / 2);
            var info = new GameUI.GameOverInfo
            {
                Score = solo.Score,
                Coins = runCoins,
                CanDouble = runCoins > 0 && Platform.RewardedAvailable,
                CanReviveAd = goodRun && Platform.RewardedAvailable && runs - lastAdReviveRun >= 3,
                CanReviveCoins = goodRun && Save.Coins >= ReviveCost,
                ReviveCost = ReviveCost,
                Daily = mode == Mode.Daily,
            };
            rewardedOffered = info.CanReviveAd;
            rewardedWatched = false;
            if (mode == Mode.Daily)
            {
                if (Daily.RecordHeight(solo.Score)) dailyJustDone = true;
                info.DailyBest = Daily.ChallengeBest;
                info.DailyGoal = Daily.Goal;
                info.DailyJustDone = dailyJustDone;
            }
            else
            {
                int oldBest = Save.Best;
                info.NewBest = solo.Score > oldBest;
                info.Unlock = "";
                if (info.NewBest)
                {
                    Save.Best = solo.Score;
                    foreach (var c in Characters.All)
                        if (c.Unlock > oldBest && c.Unlock <= solo.Score) info.Unlock = "NEW CHARACTER: " + c.Name + "!";
                    fx.Confetti(solo.Player.transform.position + Vector3.up * 2f, Theme.Palette);
                    Platform.HappyTime();
                }
            }
            info.Best = Save.Best;
            ui.ShowGameOver(info);
        }

        void EnterVersusResult()
        {
            Platform.GameplayStop();
            state = State.Result;
            stateTime = 0f;
            int winner = p1.Score == p2.Score ? -1 : p1.Score > p2.Score ? 0 : 1;
            sfx.Fanfare();
            if (winner >= 0)
            {
                var w = winner == 0 ? p1 : p2;
                fx.Confetti(w.Player.transform.position + Vector3.up * 2f, new[] { winner == 0 ? GameUI.P1Color : GameUI.P2Color, Color.white, GameUI.Gold });
            }
            ui.ShowVersusResult(winner, p1.Score, p2.Score);
        }

        void ToggleSound()
        {
            Platform.SetUserMuted(!Platform.UserMuted);
            ui.SetMuted(Platform.UserMuted);
            sfx.Click();
        }

        // ---------- Lane events ----------

        void OnPassed(Lane lane, bool perfect, int gained)
        {
            if (tutorial)
            {
                tutorial = false;
                ui.ShowTutorial(false);
            }
            var top = lane.TopY + Tower.BlockH;
            ui.PopupAt(lane.Rig.Cam, new Vector3(lane.X + 1.2f, top + 0.5f, 0f), "+" + gained, perfect ? GameUI.Gold : Color.white);
            if (perfect) sfx.Perfect(lane.Streak);

            if (mode == Mode.Versus)
            {
                ui.SetVersusScore(lane == p1 ? 0 : 1, lane.Score);
                var other = lane == p1 ? p2 : p1;
                if (other.Out && lane.Score > other.Score) EndVersus();
                return;
            }

            ui.SetScore(lane.Score);
            ui.SetRunCoins(lane.Coins);
            if (perfect) ui.Perfect(lane.Streak);
            if (mode == Mode.Daily && !Daily.ChallengeDone && !dailyJustDone && lane.Score >= Daily.Goal)
            {
                dailyJustDone = Daily.RecordHeight(lane.Score);
                if (dailyJustDone)
                {
                    ui.Banner("GOAL REACHED! +" + Daily.ChallengeReward);
                    sfx.Fanfare();
                    fx.Confetti(lane.Player.transform.position + Vector3.up * 2f, Theme.Palette);
                }
            }
        }

        void OnHit(Lane lane)
        {
            sfx.Hit();
            lane.Rig.Shake(0.45f);
            fx.Sparkle(lane.Player.transform.position + Vector3.up * 0.4f, Color.white, 14);

            if (mode != Mode.Versus)
            {
                state = State.Ending;
                stateTime = 0f;
                Platform.GameplayStop();
                return;
            }
            var other = lane == p1 ? p2 : p1;
            if (other.Out || other.Score > lane.Score) EndVersus();
        }

        void EndVersus()
        {
            if (state != State.Playing) return;
            p1.Active = p2.Active = false;
            state = State.Ending;
            stateTime = 0f;
        }

        void OnSettled(Lane lane, bool perfect, float offset)
        {
            fx.Dust(new Vector3(lane.X + offset, lane.TopY - Tower.BlockH, 0f), Color.Lerp(scenery.Current.Fog, Color.white, 0.5f));
            if (perfect)
            {
                fx.Sparkle(new Vector3(lane.X, lane.TopY + 0.1f, 0), Theme.Palette[0], 30);
                fx.Ring(new Vector3(lane.X, lane.TopY + 0.02f, 0));
            }
            // Trailer: never miss, but vary the timing so blocks land unevenly with some PERFECTs.
            if (autoplay) lane.AutoLead = soak ? 0.1f : trailer ? Random.Range(0.11f, 0.24f) : Random.value < 0.08f ? 0.01f : Random.Range(0.07f, 0.34f);

            if (mode != Mode.Classic || state != State.Playing) return;
            int next = (startWorld + lane.Tower.Height / BlocksPerWorld) % Themes.All.Length;
            if (next != world)
            {
                world = next;
                if (world > Save.MaxWorld) Save.MaxWorld = world;
                scenery.SetWorld(Theme, false);
                ui.Banner(Theme.Name);
                sfx.World();
            }
        }

        // ---------- Loop ----------

        void Update()
        {
            if (state == State.Boot || Platform.AdPlaying) return;
            float dt = Time.deltaTime;
            stateTime += dt;
            if (shot != "")
            {
                shotT += dt;
                if (shotT > 0.6f)
                {
                    shotT = 0f;
                    fx.Sparkle(solo.Player.transform.position + Vector3.up * 0.5f, Theme.Palette[0], 18);
                }
                solo.Tick(dt, false, false);
                return;
            }

            ReadInput(out bool any, out bool left, out bool right, out bool released);
            if (autoplay && !trailer) AutoMenu(ref released);

            switch (state)
            {
                case State.Menu:
                    if (released && stateTime > 0.3f && !ui.RewardOpen && Economy.Owns(charIndex)) StartRun(Mode.Classic);
                    break;
                case State.Ending:
                    if (stateTime > 1.1f)
                    {
                        if (mode == Mode.Versus) EnterVersusResult();
                        else EnterSoloResult();
                    }
                    break;
                case State.Result:
                    if (KeyReleased() && stateTime > 0.6f)
                    {
                        if (mode == Mode.Versus) Rematch();
                        else Retry();
                    }
                    break;
            }

            bool playing = state == State.Playing;
            if (playing && mode == Mode.Versus && countdownShown < 4)
            {
                int step = Mathf.Min(3, Mathf.FloorToInt(stateTime / (CountdownTime / 3f)));
                if (step > countdownShown)
                {
                    countdownShown = step;
                    ui.Banner(step < 3 ? (3 - step).ToString() : "GO!", 200, 1f);
                    if (step < 3) sfx.Click();
                    else
                    {
                        sfx.Perfect(1);
                        countdownShown = 4;
                    }
                }
            }
            if (mode == Mode.Versus && cam2.enabled)
            {
                p1.Tick(dt, playing && left, autoplay);
                p2.Tick(dt, playing && right, autoplay);
            }
            else solo.Tick(dt, playing && any, autoplay);
        }

        void ReadInput(out bool any, out bool left, out bool right, out bool released)
        {
            any = left = right = false;
            released = (PointerReleased() && !OverUI()) || KeyReleased();
            var k = Keyboard.current;
            if (k != null)
            {
                bool w = k.wKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
                bool up = k.upArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame;
                bool space = k.spaceKey.wasPressedThisFrame;
                left |= w;
                right |= up;
                any |= w || up || space;
            }

            // Every new touch counts, so both players can tap at the same time.
            var touch = Touchscreen.current;
            if (touch != null)
            {
                foreach (var t in touch.touches)
                {
                    if (!t.press.wasPressedThisFrame) continue;
                    if (OverUI()) continue;
                    any = true;
                    if (t.position.ReadValue().x < Screen.width * 0.5f) left = true;
                    else right = true;
                }
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !OverUI())
            {
                any = true;
                if (mouse.position.ReadValue().x < Screen.width * 0.5f) left = true;
                else right = true;
            }
        }

        // ---------- Cover images (?shot=1&world=N&char=N) ----------

        int shotChar;
        float shotT;

        // Poses a frozen hero moment for store covers: an uneven tower, the character mid-jump
        // looking at an incoming block, sparkles, and only the title as UI.
        void SetupShot()
        {
            state = State.Busy;
            mode = Mode.Classic;
            world = Mathf.Max(0, forcedWorld);
            scenery.SetWorld(Theme, true);
            ShowSoloLane(Characters.All[shotChar], false);
            var rng = new System.Random(7);
            for (int i = 1; i <= 6; i++)
            {
                var b = solo.Tower.CreateBlock(Theme, i);
                b.transform.SetPositionAndRotation(new Vector3(((float)rng.NextDouble() - 0.5f) * 0.36f, 0, 0),
                    Quaternion.Euler(0, ((float)rng.NextDouble() - 0.5f) * 10f, 0));
                solo.Tower.Add(b);
            }
            var incoming = solo.Tower.CreateBlock(Theme, 7);
            incoming.transform.position = new Vector3(2.1f, solo.TopY, 0);
            rig.Distance = 6.4f;
            rig.Snap(solo.TopY);

            // Coins floating around the hero.
            var coinSpots = new[] { new Vector3(-1.7f, 1.7f, 0.3f), new Vector3(1.5f, 2.3f, 0.6f), new Vector3(-1.2f, 2.9f, 1.2f) };
            foreach (var spot in coinSpots)
            {
                var anchor = new GameObject("CoverCoin").transform;
                anchor.position = new Vector3(spot.x, solo.TopY + spot.y - Tower.BlockH - 0.42f, spot.z);
                Lane.MakeCoin(anchor).transform.localScale = Vector3.one * 1.3f;
            }

            var p = solo.Player;
            p.transform.position = new Vector3(0, solo.TopY + 0.7f, 0);
            p.Rig.ArmsUp(0.8f);
            p.Rig.Look(1f);
            p.Rig.Squash.localScale = new Vector3(0.93f, 1.12f, 0.93f);
            p.enabled = false;
            ui.ShowShot(Screen.height > Screen.width);
#if UNITY_EDITOR
            var capture = System.Environment.GetEnvironmentVariable("SJ_CAPTURE");
            if (!string.IsNullOrEmpty(capture))
            {
                var parts = capture.Split('|');
                var shooter = gameObject.AddComponent<CoverShooter>();
                shooter.Cam = cam;
                shooter.OutDir = parts[0];
                shooter.Prefix = parts.Length > 1 ? parts[1] : "cover";
            }
#endif
        }

        // ---------- Trailer (driven by TrailerDirector) ----------

        // A solo run that starts partway up an uneven tower, so the clip reaches a world change.
        public void TrailerSolo(int startAt, int preStack)
        {
            state = State.Menu;
            startWorld = startAt;
            StartRun(Mode.Classic);
            var rng = new System.Random(3);
            for (int i = 1; i <= preStack; i++)
            {
                var b = solo.Tower.CreateBlock(Theme, i);
                b.transform.SetPositionAndRotation(new Vector3(((float)rng.NextDouble() - 0.5f) * 0.4f, 0, 0),
                    Quaternion.Euler(0, ((float)rng.NextDouble() - 0.5f) * 10f, 0));
                solo.Tower.Add(b);
            }
            solo.Score = preStack + 6;
            solo.Player.ResetTo(solo.TopY);
            rig.Snap(solo.TopY);
            ui.SetScore(solo.Score, false);
        }

        public void TrailerVersus(int atWorld)
        {
            state = State.Busy;
            startWorld = atWorld;
            StartVersus(0f);
        }

        // Called by CoverShooter right before each capture.
        public void ShotBurst()
        {
            var pos = solo.Player.transform.position + Vector3.up * 0.5f;
            fx.Sparkle(pos, Theme.Palette[0], 36);
            fx.Sparkle(pos, GameUI.Gold, 20);
        }

        // ---------- Test hooks ----------

        void ReadTestParams()
        {
            string url = Application.absoluteURL ?? "";
#if UNITY_EDITOR
            // Headless editor runs (cover capture) pass the same flags through an env variable.
            if (url == "") url = "?" + (System.Environment.GetEnvironmentVariable("SJ_ARGS") ?? "");
#endif
            int q = url.IndexOf('?');
            if (q < 0) return;
            foreach (var part in url.Substring(q + 1).Split('&'))
            {
                var kv = part.Split('=');
                if (kv[0] == "autoplay") autoplay = true;
                if (kv[0] == "soak") soak = true;
                if (kv[0] == "trailer") trailer = autoplay = true;
                if (kv[0] == "mode" && kv.Length > 1) forcedMode = kv[1];
                if (kv[0] == "shot") shot = kv.Length > 1 ? kv[1] : "1";
                if (kv[0] == "video") ui.SetCleanCapture();
                if (kv[0] == "char" && kv.Length > 1 && int.TryParse(kv[1], out int c))
                    shotChar = Mathf.Clamp(c, 0, Characters.All.Length - 1);
                if (kv[0] == "world" && kv.Length > 1 && int.TryParse(kv[1], out int w))
                    forcedWorld = Mathf.Clamp(w, 0, Themes.All.Length - 1);
            }
        }

        void AutoMenu(ref bool released)
        {
            if (state == State.Menu && stateTime > 1.2f)
            {
                if (forcedMode == "versus") StartVersus();
                else if (forcedMode == "daily") StartRun(Mode.Daily);
                else released = true;
            }
            if (state == State.Result && stateTime > 1.5f)
            {
                if (mode == Mode.Versus) Rematch();
                else Retry();
            }
        }

        // ---------- Input helpers ----------

        static bool PointerReleased() => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

        static bool KeyReleased()
        {
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasReleasedThisFrame || k.enterKey.wasReleasedThisFrame);
        }

        static bool OverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
