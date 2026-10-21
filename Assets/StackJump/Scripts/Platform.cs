using System;
using System.Collections;
using CrazyGames;
using UnityEngine;

namespace StackJump
{
    // Thin wrapper over the CrazyGames SDK. Falls back to PlayerPrefs and no-op ads when the SDK
    // isn't available, and never lets a missing SDK callback (ad blockers, network errors) leave
    // the game stuck: init and ads all have timeouts.
    public static class Platform
    {
        const float InitTimeout = 5f;
        const float AdStartTimeout = 8f;
        const float AdMaxDuration = 90f;

        static bool ready;
        static bool inGameplay;
        static bool sdkMuted;
        static float lastMidgame = -999f;
        static Runner runner;

        public static bool Ready => ready;
        public static bool AdPlaying { get; private set; }
        public static bool UserMuted { get; private set; }

        // Hosts the timeout coroutines; unaffected by Time.timeScale.
        class Runner : MonoBehaviour { }

        static Runner GetRunner()
        {
            if (runner != null) return runner;
            var go = new GameObject("PlatformRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return runner = go.AddComponent<Runner>();
        }

        static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }

        public static void Init(Action onReady)
        {
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                onReady?.Invoke();
            }

            if (!CrazySDK.IsAvailable)
            {
                Finish();
                return;
            }
            // If the SDK script is blocked or slow, start the game offline instead of hanging.
            GetRunner().StartCoroutine(After(InitTimeout, () =>
            {
                if (!done) Debug.LogWarning("[Platform] SDK init timed out; continuing without it.");
                Finish();
            }));
            try
            {
                CrazySDK.Init(() =>
                {
                    ready = true;
                    Safe(() => CrazySDK.Game.AddSettingsChangeListener(s =>
                    {
                        sdkMuted = s.muteAudio;
                        ApplyAudio();
                    }));
                    Finish();
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] SDK init failed: " + e.Message);
                Finish();
            }
        }

        static void Safe(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogWarning("[Platform] SDK call failed: " + e.Message); }
        }

        public static void GameplayStart()
        {
            if (inGameplay) return;
            inGameplay = true;
            if (ready) Safe(() => CrazySDK.Game.GameplayStart());
        }

        public static void GameplayStop()
        {
            if (!inGameplay) return;
            inGameplay = false;
            if (ready) Safe(() => CrazySDK.Game.GameplayStop());
        }

        public static void HappyTime()
        {
            if (ready) Safe(() => CrazySDK.Game.HappyTime());
        }

        // Between runs. CrazyGames also rate-limits, but keep them sparse for retention.
        public static void Midgame(Action done)
        {
            if (!ready || Time.realtimeSinceStartup - lastMidgame < 90f)
            {
                done?.Invoke();
                return;
            }
            lastMidgame = Time.realtimeSinceStartup;
            RequestAd(CrazyAdType.Midgame, _ => done?.Invoke());
        }

        public static void Rewarded(Action<bool> done)
        {
            if (!ready)
            {
                done?.Invoke(true); // no ads off-platform: grant the reward so the flow is testable
                return;
            }
            RequestAd(CrazyAdType.Rewarded, done);
        }

        static void RequestAd(CrazyAdType type, Action<bool> done)
        {
            bool wasInGameplay = inGameplay;
            bool started = false, finished = false;
            void Finish(bool ok)
            {
                if (finished) return;
                finished = true;
                SetAdPause(false);
                if (ok && wasInGameplay) GameplayStart();
                done?.Invoke(ok);
            }

            var r = GetRunner();
            r.StartCoroutine(After(AdStartTimeout, () => { if (!started) Finish(false); }));
            r.StartCoroutine(After(AdMaxDuration, () => Finish(false)));
            try
            {
                CrazySDK.Ad.RequestAd(type,
                    () =>
                    {
                        if (finished) return;
                        started = true;
                        SetAdPause(true);
                    },
                    _ => Finish(false),
                    () => Finish(true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Ad request failed: " + e.Message);
                Finish(false);
            }
        }

        static void SetAdPause(bool paused)
        {
            AdPlaying = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused) GameplayStop();
            ApplyAudio();
        }

        public static void SetUserMuted(bool muted)
        {
            UserMuted = muted;
            SetInt("muted", muted ? 1 : 0);
            ApplyAudio();
        }

        public static void LoadAudioPref()
        {
            UserMuted = GetInt("muted", 0) == 1;
            ApplyAudio();
        }

        static void ApplyAudio() => AudioListener.volume = UserMuted || sdkMuted || AdPlaying ? 0f : 1f;

        // ---------- Save data (CrazyGames cloud save when available) ----------

        public static int GetInt(string key, int fallback = 0)
        {
            if (ready)
            {
                try { return CrazySDK.Data.GetInt(key, fallback); }
                catch (Exception e) { Debug.LogWarning("[Platform] Data read failed: " + e.Message); }
            }
            return PlayerPrefs.GetInt(key, fallback);
        }

        public static void SetInt(string key, int value)
        {
            if (ready)
            {
                try
                {
                    CrazySDK.Data.SetInt(key, value);
                    return;
                }
                catch (Exception e) { Debug.LogWarning("[Platform] Data write failed: " + e.Message); }
            }
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
