using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace StackJump
{
    // Game loop: blocks slide in from the sides and the player taps to hop over them. A cleared
    // block stops where the timing puts it (dead center = PERFECT), so the tower grows uneven.
    // Getting hit ends the run (one rewarded revive per run). Worlds change every 20 blocks.
    [DefaultExecutionOrder(100)]
    public class GameManager : MonoBehaviour
    {
        enum State { Boot, Menu, Playing, Dying, GameOver, Busy }

        class Incoming
        {
            public GameObject Go;
            public float X, Dir, Speed, Target;
            public bool Passed, Perfect;
        }

        const int BlocksPerWorld = 20;
        const float PerfectWindow = 0.14f;
        const float MaxOffset = 0.34f;
        const float Contact = Tower.BlockSize * 0.5f + Player.Radius;

        Camera cam;
        CameraRig rig;
        Scenery scenery;
        Tower tower;
        Player player;
        GameUI ui;
        Fx fx;
        Sfx sfx;
        Transform frame, shadow;
        Material frameMat;

        State state = State.Boot;
        int score, streak, charIndex, world, startWorld, runs;
        bool revived;
        float stateTime, spawnTimer;
        Incoming incoming;
        bool autoplay; // test hook: ?autoplay&world=N in the page URL
        float autoLead = 0.1f;
        bool soak; // with autoplay: never miss, to climb through every world
        int forcedWorld = -1;

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
            tower = new GameObject("Tower").AddComponent<Tower>();
            fx = gameObject.AddComponent<Fx>();
            fx.Init();
            sfx = gameObject.AddComponent<Sfx>();
            sfx.Init();
            ui = gameObject.AddComponent<GameUI>();
            ui.Init();
            ui.OnPlay = StartPlaying;
            ui.OnPrevChar = () => CycleChar(-1);
            ui.OnNextChar = () => CycleChar(1);
            ui.OnPrevWorld = () => CycleWorld(-1);
            ui.OnNextWorld = () => CycleWorld(1);
            ui.OnRevive = Revive;
            ui.OnRetry = Retry;
            ui.OnHome = () => { sfx.Click(); EnterMenu(); };
            ui.OnSound = ToggleSound;

            player = new GameObject("Player").AddComponent<Player>();
            player.Init(SupportY, OnLanded);

            frameMat = Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 3000, 0f, null, new Color(1, 1, 1, 0.6f));
            var f = new GameObject("Frame");
            Mats.AddRenderer(f, MeshFactory.Frame(Tower.BlockSize + 0.2f, 0.04f), frameMat);
            frame = f.transform;
            var sh = new GameObject("Shadow");
            Mats.AddRenderer(sh, MeshFactory.FloorQuad(), Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 2990, 0f, Tex.Soft, new Color(0.05f, 0.02f, 0.12f, 0.3f)));
            shadow = sh.transform;

            ReadTestParams();
            world = Mathf.Max(0, forcedWorld);
            scenery.SetWorld(Theme, true);
            scenery.SetGround(Theme);
            tower.Add(tower.CreateBlock(Theme, 0));
            player.SetCharacter(Characters.All[0], false);
            player.ResetTo(tower.TopY);
            rig.Snap(tower.TopY);
        }

        void Start() => Platform.Init(Boot);

        void Boot()
        {
            Platform.LoadAudioPref();
            ui.SetMuted(Platform.UserMuted);
            charIndex = Mathf.Clamp(Save.Character, 0, Characters.All.Length - 1);
            if (!Char.IsUnlocked(Save.Best)) charIndex = 0;
            startWorld = forcedWorld >= 0 ? forcedWorld : Mathf.Clamp(Save.World, 0, Mathf.Min(Save.MaxWorld, Themes.All.Length - 1));
            EnterMenu();
        }

        // ---------- States ----------

        void ResetRun()
        {
            if (incoming != null) Destroy(incoming.Go);
            incoming = null;
            tower.Clear();
            if (world != startWorld)
            {
                world = startWorld;
                scenery.SetWorld(Theme, false);
            }
            scenery.SetGround(Theme);
            tower.Add(tower.CreateBlock(Theme, 0));
            player.ResetTo(tower.TopY);
            player.LookAt(0f);
            rig.Snap(tower.TopY);
            score = 0;
            streak = 0;
            revived = false;
        }

        void EnterMenu()
        {
            Platform.GameplayStop();
            state = State.Menu;
            stateTime = 0f;
            ResetRun();
            RefreshMenu(true);
        }

        void RefreshMenu(bool rebuildCharacter)
        {
            bool unlocked = Char.IsUnlocked(Save.Best);
            if (rebuildCharacter) player.SetCharacter(Char, !unlocked);
            int maxWorld = Mathf.Min(Save.MaxWorld, Themes.All.Length - 1);
            ui.ShowMenu(new GameUI.MenuInfo
            {
                Best = Save.Best,
                CharName = unlocked ? Char.Name : "???",
                CharLock = unlocked ? "" : "REACH " + Char.Unlock + " TO UNLOCK",
                CharLocked = !unlocked,
                WorldName = Theme.Name,
                WorldArrows = maxWorld > 0,
            });
        }

        void CycleChar(int delta)
        {
            sfx.Click();
            charIndex = (charIndex + delta + Characters.All.Length) % Characters.All.Length;
            if (Char.IsUnlocked(Save.Best)) Save.Character = charIndex;
            RefreshMenu(true);
        }

        void CycleWorld(int delta)
        {
            sfx.Click();
            int count = Mathf.Min(Save.MaxWorld, Themes.All.Length - 1) + 1;
            startWorld = (startWorld + delta + count) % count;
            Save.World = startWorld;
            ResetRun();
            RefreshMenu(false);
        }

        void StartPlaying()
        {
            if (state != State.Menu && state != State.GameOver) return;
            if (!Char.IsUnlocked(Save.Best))
            {
                ui.Banner("LOCKED!");
                sfx.Click();
                return;
            }
            sfx.Click();
            state = State.Playing;
            stateTime = 0f;
            spawnTimer = 0.5f;
            runs++;
            ui.ShowHud(Save.Best);
            ui.Banner(Theme.Name);
            Platform.GameplayStart();
        }

        void Retry()
        {
            if (state != State.GameOver) return;
            sfx.Click();
            state = State.Busy;
            void Go()
            {
                state = State.GameOver;
                ResetRun();
                player.SetCharacter(Char, false);
                StartPlaying();
            }
            if (runs >= 2) Platform.Midgame(Go);
            else Go();
        }

        void Revive()
        {
            if (state != State.GameOver || revived) return;
            sfx.Click();
            state = State.Busy;
            Platform.Rewarded(ok =>
            {
                if (!ok)
                {
                    state = State.GameOver;
                    ui.Banner("NO AD AVAILABLE");
                    return;
                }
                revived = true;
                player.ResetTo(tower.TopY);
                player.Kick(0.6f);
                fx.Sparkle(player.transform.position + Vector3.up * 0.5f, Color.white, 30);
                state = State.Playing;
                stateTime = 0f;
                spawnTimer = 1.1f;
                ui.ShowHud(Save.Best);
                ui.SetScore(score, false);
                ui.Banner("KEEP GOING!");
                Platform.GameplayStart();
            });
        }

        void EnterGameOver()
        {
            Platform.GameplayStop();
            state = State.GameOver;
            stateTime = 0f;
            int oldBest = Save.Best;
            bool newBest = score > oldBest;
            string unlock = "";
            if (newBest)
            {
                Save.Best = score;
                foreach (var c in Characters.All)
                    if (c.Unlock > oldBest && c.Unlock <= score) unlock = "NEW CHARACTER: " + c.Name + "!";
                fx.Confetti(player.transform.position + Vector3.up * 2f, Theme.Palette);
                Platform.HappyTime();
            }
            ui.ShowGameOver(score, Save.Best, newBest, unlock, !revived && score >= 3);
        }

        void ToggleSound()
        {
            Platform.SetUserMuted(!Platform.UserMuted);
            ui.SetMuted(Platform.UserMuted);
            sfx.Click();
        }

        // ---------- Loop ----------

        void Update()
        {
            if (state == State.Boot || state == State.Busy || Platform.AdPlaying) return;
            float dt = Time.deltaTime;
            stateTime += dt;
            bool pressed = (PointerPressed() && !OverUI()) || KeyPressed();
            bool released = (PointerReleased() && !OverUI()) || KeyReleased();
            if (autoplay) AutoInput(ref pressed, ref released);

            switch (state)
            {
                case State.Menu:
                    if (released && stateTime > 0.3f) StartPlaying();
                    break;
                case State.Playing:
                    if (pressed && player.Jump()) sfx.Jump();
                    UpdateIncoming(dt);
                    break;
                case State.Dying:
                    UpdateIncoming(dt);
                    if (stateTime > 1.1f) EnterGameOver();
                    break;
                case State.GameOver:
                    if (KeyReleased() && stateTime > 0.6f) Retry();
                    break;
            }

            rig.TargetY = tower.TopY;
            UpdateFrameAndShadow();
        }

        void UpdateIncoming(float dt)
        {
            if (incoming == null)
            {
                if (state == State.Playing && player.Grounded)
                {
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f) Spawn();
                }
                player.LookAt(0f);
                return;
            }

            float prevDist = Mathf.Abs(incoming.X);
            bool arrived;
            if (incoming.Passed || state != State.Playing)
            {
                incoming.X = Mathf.MoveTowards(incoming.X, incoming.Target, incoming.Speed * dt);
                arrived = Mathf.Approximately(incoming.X, incoming.Target);
            }
            else
            {
                incoming.X -= incoming.Dir * incoming.Speed * dt;
                arrived = false;
                float dist = Mathf.Abs(incoming.X);
                if (prevDist > Contact && dist <= Contact)
                {
                    if (player.FeetY >= tower.TopY + Tower.BlockH - 0.03f) Pass();
                    else Hit();
                }
            }

            incoming.Go.transform.position = new Vector3(incoming.X, tower.TopY, 0f);
            if (!player.Dead) player.LookAt(incoming.Passed ? 0f : incoming.Dir);
            if (arrived) Settle();
        }

        void Spawn()
        {
            int h = tower.Height;
            float speed = Mathf.Min(3.4f + h * 0.055f, 7.2f);
            if (h > 6) speed *= Random.Range(0.88f, 1.12f);
            if (h > 14 && Random.value < 0.15f) speed *= 1.3f;
            float dir = Random.value < 0.5f ? -1f : 1f;
            float dist = Mathf.Clamp(rig.HalfWidth + 1.0f, 3.6f, 7.5f);
            var go = tower.CreateBlock(Theme, h);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(-5f, 5f), 0);
            incoming = new Incoming { Go = go, X = dir * dist, Dir = dir, Speed = speed, Target = 0f };
            autoLead = soak ? 0.1f : Random.value < 0.08f ? 0.01f : Random.Range(0.07f, 0.34f);
            go.transform.position = new Vector3(incoming.X, tower.TopY, 0f);
        }

        void Pass()
        {
            incoming.Passed = true;
            float late = player.Grounded ? 1f : player.TimeSinceJump;
            bool perfect = late <= PerfectWindow;
            incoming.Perfect = perfect;
            // The worse the timing, the further off-center the block comes to rest.
            float quality = Mathf.Clamp01((late - PerfectWindow) / 0.28f);
            float side = Random.value < 0.65f ? -incoming.Dir : incoming.Dir;
            incoming.Target = perfect ? 0f : side * Mathf.Lerp(0.07f, MaxOffset, quality) * Random.Range(0.75f, 1f);

            streak = perfect ? streak + 1 : 0;
            int gained = perfect ? 1 + Mathf.Min(streak, 2) : 1;
            score += gained;
            ui.SetScore(score);
            var top = tower.TopY + Tower.BlockH;
            ui.PopupAt(cam, new Vector3(1.2f, top + 0.5f, 0f), "+" + gained, perfect ? new Color(1f, 0.89f, 0.36f) : Color.white);
            if (perfect)
            {
                ui.Perfect(streak);
                sfx.Perfect(streak);
            }
        }

        void Hit()
        {
            state = State.Dying;
            stateTime = 0f;
            streak = 0;
            player.Die(-incoming.Dir);
            sfx.Hit();
            rig.Shake(0.45f);
            fx.Sparkle(player.transform.position + Vector3.up * 0.4f, Color.white, 14);
            Platform.GameplayStop();
        }

        void Settle()
        {
            var block = incoming.Go;
            bool perfect = incoming.Perfect;
            float x = incoming.X;
            tower.Add(block);
            incoming = null;
            spawnTimer = Random.Range(0.12f, 0.45f);
            tower.Kick(perfect ? 8f : 22f * Mathf.Sign(x == 0f ? 1f : x));
            fx.Dust(new Vector3(x, tower.TopY - Tower.BlockH, 0f), Color.Lerp(scenery.Current.Fog, Color.white, 0.5f));
            if (perfect)
            {
                fx.Sparkle(new Vector3(0, tower.TopY + 0.1f, 0), Theme.Palette[0], 30);
                fx.Ring(new Vector3(0, tower.TopY + 0.02f, 0));
            }

            int next = (startWorld + tower.Height / BlocksPerWorld) % Themes.All.Length;
            if (next != world && state == State.Playing)
            {
                world = next;
                if (world > Save.MaxWorld) Save.MaxWorld = world;
                scenery.SetWorld(Theme, false);
                ui.Banner(Theme.Name);
                sfx.World();
            }
        }

        void OnLanded()
        {
            var feet = player.transform.position;
            sfx.Land();
            fx.Dust(feet, Color.Lerp(scenery.Current.Fog, Color.white, 0.5f));
        }

        float SupportY()
        {
            if (incoming != null && incoming.Passed) return tower.TopY + Tower.BlockH;
            return tower.TopY;
        }

        void UpdateFrameAndShadow()
        {
            bool playing = state == State.Playing;
            frame.gameObject.SetActive(playing);
            if (playing)
            {
                frame.position = new Vector3(0, tower.TopY + Tower.BlockH + 0.02f, 0);
                float a = incoming != null && !incoming.Passed ? 0.35f + Mathf.PingPong(Time.time * 2f, 0.4f) : 0.2f;
                frameMat.SetColor("_BaseColor", new Color(1, 1, 1, a));
            }

            bool showShadow = !player.Dead;
            shadow.gameObject.SetActive(showShadow);
            if (showShadow)
            {
                float ground = SupportY();
                float h = Mathf.Max(0f, player.FeetY - ground);
                float s = Mathf.Lerp(1.05f, 0.55f, h / 1.6f);
                shadow.position = new Vector3(0, ground + 0.015f, 0);
                shadow.localScale = new Vector3(s, 1f, s * 0.85f);
            }
        }

        // ---------- Test hooks ----------

        void ReadTestParams()
        {
            string url = Application.absoluteURL ?? "";
            int q = url.IndexOf('?');
            if (q < 0) return;
            foreach (var part in url.Substring(q + 1).Split('&'))
            {
                var kv = part.Split('=');
                if (kv[0] == "autoplay") autoplay = true;
                if (kv[0] == "soak") soak = true;
                if (kv[0] == "world" && kv.Length > 1 && int.TryParse(kv[1], out int w))
                    forcedWorld = Mathf.Clamp(w, 0, Themes.All.Length - 1);
            }
        }

        void AutoInput(ref bool pressed, ref bool released)
        {
            if (state == State.GameOver && stateTime > 1.2f)
            {
                Retry();
                return;
            }
            if (state != State.Playing)
            {
                released |= stateTime > 1.2f;
                return;
            }
            if (incoming != null && !incoming.Passed && player.Grounded)
                pressed |= (Mathf.Abs(incoming.X) - Contact) / incoming.Speed <= autoLead;
        }

        // ---------- Input ----------

        static bool PointerPressed() => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        static bool PointerReleased() => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

        static bool KeyPressed()
        {
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame);
        }

        static bool KeyReleased()
        {
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasReleasedThisFrame || k.enterKey.wasReleasedThisFrame);
        }

        static bool OverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
