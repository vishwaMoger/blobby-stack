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

        public static int Best
        {
            get => Get("best");
            set => Set("best", value);
        }

        public static int Character
        {
            get => Get("char");
            set => Set("char", value);
        }

        // Highest world index reached; worlds up to this one can be picked as a starting point.
        public static int MaxWorld
        {
            get => Get("maxWorld");
            set => Set("maxWorld", value);
        }

        public static int World
        {
            get => Get("world");
            set => Set("world", value);
        }
    }
}
