using UnityEngine;

namespace StackJump
{
    // Local persistence. Swap for the CrazyGames SDK data module when it's integrated.
    public static class Save
    {
        public static int Best
        {
            get => PlayerPrefs.GetInt("best", 0);
            set { PlayerPrefs.SetInt("best", value); PlayerPrefs.Save(); }
        }

        public static int Character
        {
            get => PlayerPrefs.GetInt("char", 0);
            set { PlayerPrefs.SetInt("char", value); PlayerPrefs.Save(); }
        }
    }
}
