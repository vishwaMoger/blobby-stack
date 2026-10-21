using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace StackJump
{
    // Game loop: blocks slide in from the sides and the player taps to hop over them.
    // Clearing a block stacks it; getting hit ends the run. Worlds change every 20 blocks.
    [DefaultExecutionOrder(100)]
    public class GameManager : MonoBehaviour
    {
        enum State { Menu, Playing, Dying, GameOver }

        class Incoming
        {
            public GameObject Go;
            public float X, Dir, Speed;
            public bool Passed;
        }

        const int BlocksPerWorld = 20;
        const float PerfectWindow = 0.15f;
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

        State state;
        int score, streak, charIndex, world, startWorld;
        bool autoplay; // test hook: ?autoplay&world=N in the page URL
        bool landingPerfect;
        float stateTime, spawnTimer;
        Incoming incoming;

        WorldTheme Theme => Themes.All[world];
        CharacterDef Char => Characters.All[charIndex];

        void Awake()
        {
            Application.targetFrameRate = -1;
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
            ui.OnPrevChar = () => CycleChar(-1);
            ui.OnNextChar = () => CycleChar(1);
            ui.OnCharacters = EnterMenu;

            player = new GameObject("Player").AddComponent<Player>();
            player.Init(SupportY, OnLanded);

            frameMat = Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 3000, 0f, null, new Color(1, 1, 1, 0.6f));
            var f = new GameObject("Frame");
            Mats.AddRenderer(f, MeshFactory.Frame(Tower.BlockSize + 0.2f, 0.04f), frameMat);
            frame = f.transform;
            var sh = new GameObject("Shadow");
            Mats.AddRenderer(sh, MeshFactory.FloorQuad(), Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 2990, 0f, Tex.Soft, new Color(0.05f, 0.02f, 0.12f, 0.35f)));
            shadow = sh.transform;

            charIndex = Mathf.Clamp(Save.Character, 0, Characters.All.Length - 1);
            if (!Char.IsUnlocked(Save.Best)) charIndex = 0;
            ReadTestParams();
            world = startWorld;
            scenery.SetWorld(Theme, true);
        }

        void Start() => EnterMenu();

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
            tower.Add(tower.CreateBlock(Theme, 0));
            player.ResetTo(tower.TopY);
            player.LookAt(0f);
            rig.Snap(tower.TopY);
            score = 0;
            streak = 0;
            landingPerfect = false;
        }

        void EnterMenu()
        {
            state = State.Menu;
            stateTime = 0f;
            ResetRun();
            RefreshCharacter();
        }

        void RefreshCharacter()
        {
            bool unlocked = Char.IsUnlocked(Save.Best);
            player.SetCharacter(Char, !unlocked);
            ui.ShowMenu(Save.Best, unlocked ? Char.Name : "???", unlocked ? "" : "REACH " + Char.Unlock + " TO UNLOCK");
        }

        void CycleChar(int delta)
        {
            sfx.Click();
            charIndex = (charIndex + delta + Characters.All.Length) % Characters.All.Length;
            if (Char.IsUnlocked(Save.Best)) Save.Character = charIndex;
            RefreshCharacter();
        }

        void StartPlaying()
        {
            if (!Char.IsUnlocked(Save.Best))
            {
                ui.Banner("LOCKED!");
                sfx.Click();
                return;
            }
            state = State.Playing;
            stateTime = 0f;
            spawnTimer = 0.6f;
            ui.ShowHud();
            ui.Banner(Theme.Name);
        }

        void Restart()
        {
            ResetRun();
            player.SetCharacter(Char, false);
            StartPlaying();
        }

        void EnterGameOver()
        {
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
            }
            ui.ShowGameOver(score, Save.Best, newBest, unlock);
        }

        // ---------- Loop ----------

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            bool pressed = PointerPressed() || KeyPressed();
            bool released = (PointerReleased() && !OverUI()) || KeyReleased();
            if (autoplay) AutoInput(ref pressed, ref released);

            switch (state)
            {
                case State.Menu:
                    if (released && stateTime > 0.2f) StartPlaying();
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
                    if (released && stateTime > 0.6f) Restart();
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
            incoming.X -= incoming.Dir * incoming.Speed * dt;
            bool arrived = incoming.Dir > 0f ? incoming.X <= 0f : incoming.X >= 0f;
            if (arrived) incoming.X = 0f;
            float dist = Mathf.Abs(incoming.X);

            if (state == State.Playing && !incoming.Passed && prevDist > Contact && dist <= Contact)
            {
                if (player.FeetY >= tower.TopY + Tower.BlockH - 0.03f) Pass();
                else Hit();
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
            incoming = new Incoming { Go = tower.CreateBlock(Theme, h), X = dir * dist, Dir = dir, Speed = speed };
            incoming.Go.transform.position = new Vector3(incoming.X, tower.TopY, 0f);
        }

        void Pass()
        {
            incoming.Passed = true;
            bool perfect = !player.Grounded && player.TimeSinceJump <= PerfectWindow;
            streak = perfect ? streak + 1 : 0;
            int gained = perfect ? 1 + Mathf.Min(streak, 2) : 1;
            score += gained;
            landingPerfect = perfect;
            ui.SetScore(score);
            var top = tower.TopY + Tower.BlockH;
            ui.PopupAt(cam, new Vector3(1.15f, top + 0.45f, 0f), "+" + gained, perfect ? new Color(1f, 0.89f, 0.36f) : Color.white);
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
        }

        void Settle()
        {
            tower.Add(incoming.Go);
            incoming = null;
            spawnTimer = Random.Range(0.12f, 0.45f);

            int next = (startWorld + tower.Height / BlocksPerWorld) % Themes.All.Length;
            if (next != world && state == State.Playing)
            {
                world = next;
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
            if (landingPerfect && feet.y >= tower.TopY - 0.01f)
                fx.Sparkle(feet + Vector3.up * 0.4f, Theme.Palette[0]);
            landingPerfect = false;
        }

        float SupportY()
        {
            if (incoming != null && incoming.Passed && Mathf.Abs(incoming.X) < Tower.BlockSize * 0.5f + Player.Radius * 0.3f)
                return tower.TopY + Tower.BlockH;
            return tower.TopY;
        }

        void UpdateFrameAndShadow()
        {
            bool playing = state == State.Playing;
            frame.gameObject.SetActive(playing);
            if (playing)
            {
                frame.position = new Vector3(0, tower.TopY + Tower.BlockH + 0.02f, 0);
                float a = incoming != null && !incoming.Passed ? 0.35f + Mathf.PingPong(Time.time * 2f, 0.4f) : 0.25f;
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
                if (kv[0] == "world" && kv.Length > 1 && int.TryParse(kv[1], out int w))
                    startWorld = Mathf.Clamp(w, 0, Themes.All.Length - 1);
            }
        }

        void AutoInput(ref bool pressed, ref bool released)
        {
            if (state != State.Playing)
            {
                released |= stateTime > 1.2f;
                return;
            }
            if (incoming != null && !incoming.Passed && player.Grounded)
                pressed |= (Mathf.Abs(incoming.X) - Contact) / incoming.Speed <= 0.1f;
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
