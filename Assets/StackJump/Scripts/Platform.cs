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
        const float InitTimeout = 8f;
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

        // Ads are compiled in only for the Full Launch build (SJ_ADS). Basic Launch has ads
        // disabled by CrazyGames, and rewarded buttons that do nothing get a game rejected.
#if SJ_ADS
        public const bool AdsEnabled = true;
#else
        public const bool AdsEnabled = false;
#endif
        static bool adblock, adsBroken;

        // Whether a rewarded button should be shown at all: never with ad blockers, never after
        // an ad has failed this session, and never when ads aren't part of this build.
        public static bool RewardedAvailable => AdsEnabled && ready && !adblock && !adsBroken;

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
                    if (AdsEnabled) Safe(() => CrazySDK.Ad.HasAdblock(blocked => adblock = blocked));
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
            if (!AdsEnabled || !ready || adblock || Time.realtimeSinceStartup - lastMidgame < 90f)
            {
                done?.Invoke();
                return;
            }
            lastMidgame = Time.realtimeSinceStartup;
            RequestAd(CrazyAdType.Midgame, _ => done?.Invoke());
        }

        // Callers only offer rewarded ads when RewardedAvailable; an error never grants the reward.
        public static void Rewarded(Action<bool> done)
        {
            if (!RewardedAvailable)
            {
                done?.Invoke(false);
                return;
            }
            RequestAd(CrazyAdType.Rewarded, ok =>
            {
                if (!ok) adsBroken = true; // hide rewarded buttons for the rest of the session
                done?.Invoke(ok);
            });
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

        // The data source is fixed at the first read. If the SDK was late (init timed out), the
        // session keeps using the local copy; it must never write that copy over the cloud save.
        static bool? cloudData;

        static bool UseCloud => cloudData ??= ready;

        public static int GetInt(string key, int fallback = 0)
        {
            if (UseCloud)
            {
                try { return CrazySDK.Data.GetInt(key, fallback); }
                catch (Exception e) { Debug.LogWarning("[Platform] Data read failed: " + e.Message); }
            }
            return PlayerPrefs.GetInt(key, fallback);
        }

        public static void SetInt(string key, int value)
        {
            if (UseCloud)
            {
                try { CrazySDK.Data.SetInt(key, value); }
                catch (Exception e) { Debug.LogWarning("[Platform] Data write failed: " + e.Message); }
            }
            // Always keep a local copy as a fallback for sessions where the SDK is unavailable.
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
