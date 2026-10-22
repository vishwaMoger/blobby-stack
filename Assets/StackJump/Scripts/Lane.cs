using System;
using UnityEngine;

namespace StackJump
{
    // One player's tower: the incoming block, hop/hit/pass logic, coin pickups, the landing frame
    // and blob shadow. Single player uses one lane; the 2-player race runs two side by side.
    public class Lane
    {
        public class Incoming
        {
            public GameObject Go, Coin;
            public float X, Dir, Speed, Target;
            public bool Passed, Perfect;
        }

        public const float PerfectWindow = 0.14f;
        const float MaxOffset = 0.34f;
        public static readonly float Contact = Tower.BlockSize * 0.5f + Player.Radius;

        public readonly float X;
        public readonly Tower Tower;
        public readonly Player Player;
        public readonly CameraRig Rig;
        readonly Transform frame, shadow;
        readonly Material frameMat;
        readonly Func<WorldTheme> theme;

        public int Score, Streak, Coins;
        public bool Active, Out;
        // No new block spawns while held (the tutorial run waits for the player's first tap).
        public bool Hold;
        public Incoming Current { get; private set; }
        public float AutoLead = 0.1f;

        float spawnTimer;
        System.Random rng = new();
        int fixedDir;
        GameObject topCoin;

        public event Action<Lane> Jumped, Landed, Hit, CoinCollected;
        public event Action<Lane, bool, int> Passed;   // perfect, points gained
        public event Action<Lane, bool, float> Settled; // perfect, landing offset

        public float TopY => Tower.TopY;

        public Lane(string name, float x, CameraRig rig, Func<WorldTheme> themeSource)
        {
            X = x;
            Rig = rig;
            theme = themeSource;
            Tower = new GameObject(name + " Tower").AddComponent<Tower>();
            Player = new GameObject(name + " Player").AddComponent<Player>();
            Player.HomeX = x;
            Player.Init(SupportY, OnPlayerLanded);

            frameMat = Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 3000, 0f, null, new Color(1, 1, 1, 0.6f));
            var f = new GameObject(name + " Frame");
            Mats.AddRenderer(f, MeshFactory.Frame(Tower.BlockSize + 0.2f, 0.04f), frameMat);
            frame = f.transform;
            var sh = new GameObject(name + " Shadow");
            Mats.AddRenderer(sh, MeshFactory.FloorQuad(), Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 2990, 0f, Tex.Soft, new Color(0.05f, 0.02f, 0.12f, 0.3f)));
            shadow = sh.transform;
        }

        // seed < 0: random blocks; fixedSide 0: blocks from either side, -1/+1: always that side.
        public void Reset(int seed, int fixedSide, float startDelay = 0.5f)
        {
            if (Current != null) UnityEngine.Object.Destroy(Current.Go);
            Current = null;
            topCoin = null;
            Tower.Clear();
            var first = Tower.CreateBlock(theme(), 0);
            first.transform.position = new Vector3(X, 0, 0);
            Tower.Add(first);
            Player.ResetTo(TopY);
            Player.LookAt(0f);
            Rig.CenterX = X;
            Rig.Snap(TopY);
            Score = Streak = Coins = 0;
            Active = Out = false;
            rng = seed >= 0 ? new System.Random(seed) : new System.Random();
            fixedDir = fixedSide;
            spawnTimer = startDelay;
        }

        public void SetVisible(bool visible)
        {
            Tower.gameObject.SetActive(visible);
            Player.gameObject.SetActive(visible);
            if (!visible)
            {
                frame.gameObject.SetActive(false);
                shadow.gameObject.SetActive(false);
            }
        }

        // Puts the player back on top after a revive.
        public void Revive()
        {
            Out = false;
            Active = true;
            Streak = 0;
            Player.ResetTo(TopY);
            Player.Kick(0.6f);
            spawnTimer = 1.1f;
        }

        public void Tick(float dt, bool pressed, bool autoplay)
        {
            if (autoplay && Active && !Out && Current != null && !Current.Passed && Player.Grounded)
                pressed |= (Mathf.Abs(Current.X) - Contact) / Current.Speed <= AutoLead;
            if (Active && !Out && pressed && Player.Jump()) Jumped?.Invoke(this);

            UpdateIncoming(dt);
            Rig.TargetY = TopY;
            UpdateFrameAndShadow();
        }

        void UpdateIncoming(float dt)
        {
            if (Current == null)
            {
                if (Active && !Out && Player.Grounded && !Hold)
                {
                    spawnTimer -= dt;
                    if (spawnTimer <= 0f) Spawn();
                }
                if (!Player.Dead) Player.LookAt(0f);
                return;
            }

            var c = Current;
            float prevDist = Mathf.Abs(c.X);
            bool arrived = false;
            if (c.Passed || !Active || Out)
            {
                c.X = Mathf.MoveTowards(c.X, c.Target, c.Speed * dt);
                arrived = Mathf.Approximately(c.X, c.Target);
            }
            else
            {
                c.X -= c.Dir * c.Speed * dt;
                float dist = Mathf.Abs(c.X);
                if (prevDist > Contact && dist <= Contact)
                {
                    if (Player.FeetY >= TopY + Tower.BlockH - 0.03f) Pass();
                    else HitPlayer();
                }
            }

            c.Go.transform.position = new Vector3(X + c.X, TopY, 0f);
            if (!Player.Dead) Player.LookAt(c.Passed ? 0f : c.Dir);
            if (arrived) Settle();
        }

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        void Spawn()
        {
            int h = Tower.Height;
            float speed = Mathf.Min(3.4f + h * 0.055f, 7.2f);
            if (h > 6) speed *= R(0.88f, 1.12f);
            if (h > 14 && rng.NextDouble() < 0.15) speed *= 1.3f;
            float dir = fixedDir != 0 ? fixedDir : rng.NextDouble() < 0.5 ? -1f : 1f;
            float dist = Mathf.Clamp(Rig.HalfWidth + 1.0f, 3.6f, 7.5f);
            var go = Tower.CreateBlock(theme(), h);
            go.transform.rotation = Quaternion.Euler(0, R(-5f, 5f), 0);
            Current = new Incoming { Go = go, X = dir * dist, Dir = dir, Speed = speed, Target = 0f };
            if (h >= 2 && rng.NextDouble() < 0.22) Current.Coin = MakeCoin(go.transform);
            go.transform.position = new Vector3(X + Current.X, TopY, 0f);
        }

        public static GameObject MakeCoin(Transform block)
        {
            var coin = new GameObject("Coin");
            coin.transform.SetParent(block, false);
            coin.transform.localPosition = new Vector3(0, Tower.BlockH + 0.42f, 0);
            var face = new GameObject("Face");
            face.transform.SetParent(coin.transform, false);
            face.transform.localRotation = Quaternion.Euler(90, 0, 0);
            face.transform.localScale = new Vector3(0.38f, 0.035f, 0.38f);
            var r = Mats.AddRenderer(face, MeshFactory.Cylinder, Mats.Toon);
            Mats.Set(r, Mats.Hex("FFC83A"), 0.95f, 0.35f, 0f, 0f, 0.1f);
            var rim = new GameObject("Mark");
            rim.transform.SetParent(coin.transform, false);
            rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            rim.transform.localScale = new Vector3(0.22f, 0.045f, 0.22f);
            Mats.Set(Mats.AddRenderer(rim, MeshFactory.Cylinder, Mats.Toon), Mats.Hex("FFE680"), 0.95f, 0.5f, 0f, 0f, 0.1f);
            coin.AddComponent<Drifter>().Setup(180f, 2.4f, 0f, 0f);
            return coin;
        }

        void Pass()
        {
            var c = Current;
            c.Passed = true;
            float late = Player.Grounded ? 1f : Player.TimeSinceJump;
            bool perfect = late <= PerfectWindow;
            c.Perfect = perfect;
            // The worse the timing, the further off-center the block comes to rest.
            float quality = Mathf.Clamp01((late - PerfectWindow) / 0.28f);
            float side = UnityEngine.Random.value < 0.65f ? -c.Dir : c.Dir;
            c.Target = perfect ? 0f : side * Mathf.Lerp(0.07f, MaxOffset, quality) * UnityEngine.Random.Range(0.75f, 1f);

            Streak = perfect ? Streak + 1 : 0;
            int gained = perfect ? 1 + Mathf.Min(Streak, 2) : 1;
            Score += gained;
            Coins += 1;
            Passed?.Invoke(this, perfect, gained);
        }

        void HitPlayer()
        {
            Out = true;
            Streak = 0;
            Player.Die(-Current.Dir);
            Hit?.Invoke(this);
        }

        void Settle()
        {
            var c = Current;
            Tower.Add(c.Go);
            Current = null;
            if (c.Coin != null) topCoin = c.Coin;
            spawnTimer = R(0.12f, 0.45f);
            Tower.Kick(c.Perfect ? 8f : 22f * Mathf.Sign(c.X == 0f ? 1f : c.X));
            Settled?.Invoke(this, c.Perfect, c.X);
            if (Player.Grounded && !Player.Dead && Player.FeetY >= TopY - 0.01f) CollectCoin();
        }

        void OnPlayerLanded()
        {
            Landed?.Invoke(this);
            CollectCoin();
        }

        void CollectCoin()
        {
            GameObject coin = null;
            if (Current != null && Current.Passed && Current.Coin != null && Player.FeetY >= TopY + Tower.BlockH - 0.01f)
            {
                coin = Current.Coin;
                Current.Coin = null;
            }
            else if (topCoin != null && Player.FeetY >= TopY - 0.01f)
            {
                coin = topCoin;
                topCoin = null;
            }
            if (coin == null || Out) return;
            UnityEngine.Object.Destroy(coin);
            Coins += Economy.CoinPickup;
            CoinCollected?.Invoke(this);
        }

        float SupportY()
        {
            if (Current != null && Current.Passed) return TopY + Tower.BlockH;
            return TopY;
        }

        void UpdateFrameAndShadow()
        {
            bool playing = Active && !Out;
            frame.gameObject.SetActive(playing);
            if (playing)
            {
                frame.position = new Vector3(X, TopY + Tower.BlockH + 0.02f, 0);
                float a = Current != null && !Current.Passed ? 0.35f + Mathf.PingPong(Time.time * 2f, 0.4f) : 0.2f;
                frameMat.SetColor("_BaseColor", new Color(1, 1, 1, a));
            }

            bool showShadow = !Player.Dead && Player.gameObject.activeSelf;
            shadow.gameObject.SetActive(showShadow);
            if (showShadow)
            {
                float ground = SupportY();
                float h = Mathf.Max(0f, Player.FeetY - ground);
                float s = Mathf.Lerp(1.05f, 0.55f, h / 1.6f);
                shadow.position = new Vector3(X, ground + 0.015f, 0);
                shadow.localScale = new Vector3(s, 1f, s * 0.85f);
            }
        }
    }
}
