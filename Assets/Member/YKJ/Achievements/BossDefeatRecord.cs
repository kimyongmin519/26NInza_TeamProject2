using System;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements
{
    public static class BossDefeatRecord
    {
        private const string KeyPrefix = "Achievements.BossDefeats.";
        public static event Action<string> OnRecorded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetListeners() => OnRecorded = null;

        public static int GetCount(string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName)) return 0;
            return Mathf.Max(0, PlayerPrefs.GetInt(KeyPrefix + bossName.Trim(), 0));
        }

        public static void Record(string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName)) return;
            string name = bossName.Trim();
            int count = GetCount(name);
            PlayerPrefs.SetInt(KeyPrefix + name, count == int.MaxValue ? count : count + 1);
            PlayerPrefs.Save();
            OnRecorded?.Invoke(name);
        }
    }
}
