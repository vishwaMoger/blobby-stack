using System.Collections.Generic;

namespace StackJump
{
    // Persistent progress, stored through the platform (CrazyGames cloud save or PlayerPrefs).
    // Values are cached after the first read so gameplay never round-trips to JavaScript.
    public static class Save
    {
        static readonly Dictionary<string, int> cache = new();

        static int Get(string key)
        {
            if (!cache.TryGetValue(key, out int v)) cache[key] = v = Platform.GetInt(key);
            return v;
        }

        static void Set(string key, int value)
        {
            if (cache.TryGetValue(key, out int old) && old == value) return;
            cache[key] = value;
            Platform.SetInt(key, value);
        }

        public static int Best { get => Get("best"); set => Set("best", value); }
        public static int Character { get => Get("char"); set => Set("char", value); }

        // Highest world index reached; worlds up to this one can be picked as a starting point.
        public static int MaxWorld { get => Get("maxWorld"); set => Set("maxWorld", value); }
        public static int World { get => Get("world"); set => Set("world", value); }

        public static int Coins { get => Get("coins"); set => Set("coins", value); }
        public static int Owned { get => Get("owned"); set => Set("owned", value); } // character bitmask

        public static int RewardDay { get => Get("rewardDay"); set => Set("rewardDay", value); }
        public static int RewardStreak { get => Get("rewardStreak"); set => Set("rewardStreak", value); }

        public static int ChallengeDay { get => Get("chalDay"); set => Set("chalDay", value); }
        public static int ChallengeBest { get => Get("chalBest"); set => Set("chalBest", value); }
        public static int ChallengeDone { get => Get("chalDone"); set => Set("chalDone", value); }
    }
}
