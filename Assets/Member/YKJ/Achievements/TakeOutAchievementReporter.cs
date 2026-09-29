using UnityEngine;

namespace Member.Wst.Scripts.Achievements
{
    // Called only after the player successfully takes this prop into their hand.
    public sealed class TakeOutAchievementReporter : MonoBehaviour
    {
        [SerializeField] private AchievementCounter counter = AchievementCounter.BasketballTakenOut;
        private bool _reported;
        public void Report()
        {
            if (_reported) return;
            _reported = true;
            AchievementProgressRecord.Record(counter);
        }
    }
}
