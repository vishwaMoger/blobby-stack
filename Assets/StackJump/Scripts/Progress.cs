using System;

namespace StackJump
{
    // Coins and character ownership.
    public static class Economy
    {
        public const int CoinPickup = 5;

        // Free characters, ones bought with coins, and ones earned by score (older saves) are all owned.
        public static bool Owns(int index)
        {
            var c = Characters.All[index];
            return c.Price == 0 || (Save.Owned & (1 << index)) != 0 || Save.Best >= c.Unlock;
        }

        public static bool TryBuy(int index)
        {
            if (Owns(index)) return true;
            int price = Characters.All[index].Price;
            if (Save.Coins < price) return false;
            Save.Coins -= price;
            Save.Owned |= 1 << index;
            return true;
        }

        public static void Add(int amount)
        {
            if (amount > 0) Save.Coins += amount;
        }
    }

    // Day-based features: the 7-day login reward and the daily challenge tower.
    public static class Daily
    {
        public static readonly int[] Rewards = { 50, 75, 100, 150, 200, 300, 500 };
        public const int ChallengeReward = 300;

        public static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;

        // ---------- Login reward ----------

        public static bool RewardAvailable => Save.RewardDay != Today;

        // Which of the 7 days is up next; missing a day restarts the streak.
        public static int RewardIndex => Save.RewardDay == Today - 1 ? Save.RewardStreak % Rewards.Length : 0;

        public static int ClaimReward(bool doubled)
        {
            if (!RewardAvailable) return 0;
            int index = RewardIndex;
            int amount = Rewards[index] * (doubled ? 2 : 1);
            Save.RewardStreak = index + 1;
            Save.RewardDay = Today;
            Economy.Add(amount);
            return amount;
        }

        // ---------- Challenge tower ----------

        // Everyone gets the same tower today: same world, same block sequence, same goal.
        public static int Seed => Today * 7919 + 17;
        public static int World => Today % Themes.All.Length;
        public static int Goal => 25 + Today % 3 * 5;

        public static bool ChallengeDone => Save.ChallengeDay == Today && Save.ChallengeDone == 1;
        public static int ChallengeBest => Save.ChallengeDay == Today ? Save.ChallengeBest : 0;

        // Returns true the first time today's goal is reached.
        public static bool RecordHeight(int height)
        {
            if (Save.ChallengeDay != Today)
            {
                Save.ChallengeDay = Today;
                Save.ChallengeBest = 0;
                Save.ChallengeDone = 0;
            }
            if (height > Save.ChallengeBest) Save.ChallengeBest = height;
            if (height >= Goal && Save.ChallengeDone == 0)
            {
                Save.ChallengeDone = 1;
                Economy.Add(ChallengeReward);
                return true;
            }
            return false;
        }
    }
}
