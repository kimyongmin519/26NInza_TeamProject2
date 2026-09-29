using System;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements
{
    public enum AchievementCounter { BasketballTakenOut, MimicTimeoutExplosion, GameQuit }

    public static class AchievementProgressRecord
    {
        private const string Prefix = "Achievements.Counters.";
        public static event Action<AchievementCounter> OnChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            OnChanged = null;
            Application.quitting -= RecordQuit;
            Application.quitting += RecordQuit;
        }

        private static void RecordQuit() => Record(AchievementCounter.GameQuit);

        public static int GetCount(AchievementCounter counter) => Mathf.Max(0, PlayerPrefs.GetInt(Prefix + counter, 0));

        public static void Record(AchievementCounter counter)
        {
            int count = GetCount(counter);
            PlayerPrefs.SetInt(Prefix + counter, count == int.MaxValue ? count : count + 1);
            PlayerPrefs.Save();
            OnChanged?.Invoke(counter);
        }
    }
}
