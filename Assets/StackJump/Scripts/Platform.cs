using System;
using System.Collections;
#if SJ_POKI
// Poki build: PokiUnitySDK (Assets/PokiSDK) is compiled in, the CrazyGames SDK is not.
#elif SJ_GD
using System.Runtime.InteropServices;
#else
using CrazyGames;
#endif
using UnityEngine;

namespace StackJump
{
    // The one place that talks to the web portal's SDK: CrazyGames by default, Poki when built with
    // SJ_POKI (StackJumpSetup.BuildPoki), GameDistribution with SJ_GD (BuildGameDistribution).
    // The rest of the game only sees this API.
    // Missing SDK callbacks (ad blockers, network errors) can never leave the game stuck: init and
    // ads all have timeouts. Saves fall back to PlayerPrefs.
    public static class Platform
    {
        const float InitTimeout = 8f;
        const float AdStartTimeout = 8f;
        const float AdMaxDuration = 90f;

        static bool ready;
        static bool inGameplay;
        // Poki ignores ad requests until gameplayStart has been sent once, and never calls back.
        static bool gameplayReported;
        static bool sdkMuted;
        static float lastMidgame = -999f;
        static Runner runner;

        public static bool Ready => ready;
        public static bool AdPlaying { get; private set; }
        public static bool UserMuted { get; private set; }

#if SJ_POKI
        public const bool IsPoki = true, IsGameDistribution = false;
        // Poki serves ads on every build; Poki alone decides how often they actually show.
        public const bool AdsEnabled = true;
#elif SJ_GD
        public const bool IsPoki = false, IsGameDistribution = true;
        // GameDistribution rate-limits ads itself; the game may ask at every natural break.
        public const bool AdsEnabled = true;
#else
        public const bool IsPoki = false, IsGameDistribution = false;
        // Ads are compiled in only for the CrazyGames Full Launch build (SJ_ADS). Basic Launch has ads
        // disabled by CrazyGames, and rewarded buttons that do nothing get a game rejected.
#if SJ_ADS
        public const bool AdsEnabled = true;
#else
        public const bool AdsEnabled = false;
#endif
#endif
        public const bool IsCrazyGames = !IsPoki && !IsGameDistribution;
        static bool adblock, adsBroken;

        // Whether a rewarded button should be shown at all: never with ad blockers, never after
        // an ad has failed this session, and never when ads aren't part of this build.
        public static bool RewardedAvailable => AdsEnabled && ready && !adblock && !adsBroken
            && (gameplayReported || !IsPoki)
            && (gdRewardedReady || !IsGameDistribution);

        // ---------- Portal rules the game adapts to ----------

        // Poki and GameDistribution: signal every natural break and let the portal decide; no
        // frequency caps of our own. CrazyGames: keep midgames sparse, never next to a rewarded offer.
        public static bool CommercialBreakEveryRestart => !IsCrazyGames;
        // CrazyGames: the ad revive is offered at most every few runs. Poki forbids such cooldowns.
        public static bool RewardedCooldowns => IsCrazyGames;
        // Poki: no custom "ad not available" messages. GameDistribution's SDK fails silently too.
        public static bool ShowAdErrors => IsCrazyGames;
        // Poki: rewarded buttons can't be green and the normal button goes beside or above them.
        // CrazyGames: the skip button must match the reward button's size, font and color.
        public static bool RewardedButtonsDistinct => !IsCrazyGames;
        // Escape exits fullscreen on CrazyGames, so it only pauses elsewhere.
        public static bool EscapePauses => !IsCrazyGames;
        // GameDistribution: ads only on user input, so new players start from the menu's PLAY
        // (which carries the pre-roll) instead of being dropped straight into a run.
        public static bool AutoStartFirstRun => !IsGameDistribution;

        // Hosts the timeout coroutines (unaffected by Time.timeScale) and receives
        // GameDistribution bridge messages (SendMessage("PlatformRunner", "GdEvent", name)).
        class Runner : MonoBehaviour
        {
            void GdEvent(string name) => OnGdEvent(name);
        }

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

        static void Safe(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogWarning("[Platform] SDK call failed: " + e.Message); }
        }

        static Action initDone;

        public static void Init(Action onReady)
        {
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                onReady?.Invoke();
            }
            initDone = Finish;

            // If the SDK script is blocked or slow, start the game offline instead of hanging.
            GetRunner().StartCoroutine(After(InitTimeout, () =>
            {
                if (!done) Debug.LogWarning("[Platform] SDK init timed out; continuing without it.");
                Finish();
            }));

#if SJ_POKI
            try
            {
                var poki = PokiUnitySDK.Instance;
                poki.sdkInitializedCallback = () =>
                {
                    ready = true;
                    adblock = poki.adblocked;
                    Safe(() => poki.gameLoadingFinished());
                    // The mobile Poki pill sits top-left; keep it below the coin and best-score badges.
                    Safe(() => poki.movePill(0, 96));
                    Finish();
                };
                poki.init();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Poki init failed: " + e.Message);
                Finish();
            }
#elif SJ_GD
            // SDK_READY (or a load failure) arrives through OnGdEvent; queued events flush here.
            Safe(SJ_GD_Init);
#else
            if (!CrazySDK.IsAvailable)
            {
                Finish();
                return;
            }
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
#endif
        }

        public static void GameplayStart()
        {
            if (inGameplay) return;
            inGameplay = true;
            if (!ready) return;
            gameplayReported = true;
#if SJ_POKI
            Safe(() => PokiUnitySDK.Instance.gameplayStart());
#elif !SJ_GD
            Safe(() => CrazySDK.Game.GameplayStart());
#endif
        }

        public static void GameplayStop()
        {
            if (!inGameplay) return;
            inGameplay = false;
            if (!ready) return;
#if SJ_POKI
            Safe(() => PokiUnitySDK.Instance.gameplayStop());
#elif !SJ_GD
            Safe(() => CrazySDK.Game.GameplayStop());
#endif
        }

        public static void HappyTime()
        {
#if !SJ_POKI && !SJ_GD
            if (ready) Safe(() => CrazySDK.Game.HappyTime());
#endif
        }

        // GameDistribution wants a pre-roll before the player starts, triggered by their input.
        static bool prerollDone;
        public static bool PrerollPending => IsGameDistribution && !prerollDone && AdsEnabled && ready && !adblock;

        public static void Preroll(Action done)
        {
            prerollDone = true;
            Midgame(done);
        }

        // A natural break before returning to gameplay.
        public static void Midgame(Action done)
        {
            prerollDone = true;
            if (!AdsEnabled || !ready || adblock || (IsPoki && !gameplayReported))
            {
                done?.Invoke();
                return;
            }
            if (IsCrazyGames)
            {
                if (Time.realtimeSinceStartup - lastMidgame < 90f)
                {
                    done?.Invoke();
                    return;
                }
                lastMidgame = Time.realtimeSinceStartup;
            }
            RequestAd(false, _ => done?.Invoke());
        }

        // Callers only offer rewarded ads when RewardedAvailable; an error never grants the reward.
        public static void Rewarded(Action<bool> done)
        {
            if (!RewardedAvailable)
            {
                done?.Invoke(false);
                return;
            }
            RequestAd(true, ok =>
            {
                // On CrazyGames a failure means ads are broken this session. On Poki and
                // GameDistribution "no reward" also covers the player closing the ad early.
                if (!ok && IsCrazyGames) adsBroken = true;
                done?.Invoke(ok);
            });
        }

        static void RequestAd(bool rewarded, Action<bool> done)
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
            r.StartCoroutine(After(AdMaxDuration, () => Finish(false)));
#if SJ_POKI
            // Poki: mute and block input before the break; there is no separate "ad started" callback.
            started = true;
            SetAdPause(true);
            try
            {
                var poki = PokiUnitySDK.Instance;
                if (rewarded)
                {
                    poki.rewardedBreakCallBack = withReward => Finish(withReward);
                    poki.rewardedBreak();
                }
                else
                {
                    poki.commercialBreakCallBack = () => Finish(true);
                    poki.commercialBreak();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Ad request failed: " + e.Message);
                Finish(false);
            }
#elif SJ_GD
            // The showAd promise settles when the ad process ends (or is skipped by the SDK's
            // frequency limit); a reward counts only if SDK_REWARDED_WATCH_COMPLETE came first.
            started = true;
            SetAdPause(true);
            gdRewardWatched = false;
            gdPending = ok =>
            {
                if (rewarded)
                {
                    ok &= gdRewardWatched;
                    gdRewardedReady = false;
                    Safe(SJ_GD_PreloadRewarded);
                }
                Finish(ok);
            };
            try { SJ_GD_ShowAd(rewarded ? 1 : 0); }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Ad request failed: " + e.Message);
                CompleteGdAd(false);
            }
#else
            r.StartCoroutine(After(AdStartTimeout, () => { if (!started) Finish(false); }));
            try
            {
                CrazySDK.Ad.RequestAd(rewarded ? CrazyAdType.Rewarded : CrazyAdType.Midgame,
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
#endif
        }

        // ---------- GameDistribution bridge ----------

        static Action<bool> gdPending;
        static bool gdRewardWatched, gdRewardedReady, gdSdkPaused;

#if SJ_GD
        [DllImport("__Internal")] static extern void SJ_GD_Init();
        [DllImport("__Internal")] static extern void SJ_GD_ShowAd(int rewarded);
        [DllImport("__Internal")] static extern void SJ_GD_PreloadRewarded();
#else
        static void SJ_GD_PreloadRewarded() { }
#endif

        static void CompleteGdAd(bool ok)
        {
            var pending = gdPending;
            gdPending = null;
            pending?.Invoke(ok);
        }

        static void OnGdEvent(string name)
        {
            switch (name)
            {
                case "SDK_READY":
                    ready = true;
                    Safe(SJ_GD_PreloadRewarded);
                    initDone?.Invoke();
                    break;
                case "SDK_ERROR":
                case "SJ_SDK_FAILED":
                    initDone?.Invoke();
                    break;
                // The SDK can also pause on its own (e.g. an ad it starts itself): mute and freeze
                // until it says the game may continue.
                case "SDK_GAME_PAUSE":
                    if (!AdPlaying)
                    {
                        gdSdkPaused = true;
                        SetAdPause(true);
                    }
                    break;
                case "SDK_GAME_START":
                    if (gdSdkPaused && gdPending == null)
                    {
                        gdSdkPaused = false;
                        SetAdPause(false);
                    }
                    break;
                case "SDK_REWARDED_WATCH_COMPLETE":
                    gdRewardWatched = true;
                    break;
                case "SJ_AD_DONE":
                    CompleteGdAd(true);
                    break;
                case "SJ_AD_FAILED":
                    CompleteGdAd(false);
                    break;
                case "SJ_REWARDED_READY":
                    gdRewardedReady = true;
                    break;
                case "SJ_REWARDED_NONE":
                    gdRewardedReady = false;
                    break;
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

        // ---------- Save data ----------
        // CrazyGames: cloud save through the SDK when available, mirrored locally.
        // Poki and GameDistribution: local only (PlayerPrefs, which also works in incognito for the session).

        // The data source is fixed at the first read. If the SDK was late (init timed out), the
        // session keeps using the local copy; it must never write that copy over the cloud save.
        static bool? cloudData;

        static bool UseCloud => cloudData ??= ready && IsCrazyGames;

        public static int GetInt(string key, int fallback = 0)
        {
#if !SJ_POKI && !SJ_GD
            if (UseCloud)
            {
                try { return CrazySDK.Data.GetInt(key, fallback); }
                catch (Exception e) { Debug.LogWarning("[Platform] Data read failed: " + e.Message); }
            }
#endif
            try { return PlayerPrefs.GetInt(key, fallback); }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Local read failed: " + e.Message);
                return fallback;
            }
        }

        public static void SetInt(string key, int value)
        {
#if !SJ_POKI && !SJ_GD
            if (UseCloud)
            {
                try { CrazySDK.Data.SetInt(key, value); }
                catch (Exception e) { Debug.LogWarning("[Platform] Data write failed: " + e.Message); }
            }
#endif
            // Always keep a local copy as a fallback for sessions where the SDK is unavailable.
            try
            {
                PlayerPrefs.SetInt(key, value);
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[Platform] Local write failed: " + e.Message); }
        }
    }
}
