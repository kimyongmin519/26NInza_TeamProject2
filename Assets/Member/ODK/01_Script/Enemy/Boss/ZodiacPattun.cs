using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public abstract class ZodiacPattern : MonoBehaviour
    {
        protected ZodiacBossController Boss { get; private set; }

        protected float Speed =>
            Boss.PatternSpeed;

        protected ZodiacArena2D Arena =>
            Boss.Arena;

        protected System.Collections.Generic.IReadOnlyList<Transform>
            Players => Boss.Players;

        public void Initialize(
            ZodiacBossController boss)
        {
            Boss = boss;

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public abstract IEnumerator Execute();

        protected WaitForSeconds Wait(
            float duration)
        {
            return new WaitForSeconds(
                duration /
                Mathf.Max(0.01f, Speed)
            );
        }
    }
}