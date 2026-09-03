using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class VirgoPattern : ZodiacPattern
    {
        [SerializeField]
        private StarThread threadPrefab;

        [SerializeField]
        private int starCount = 7;

        [SerializeField]
        private float warningTime = 1.5f;

        [SerializeField]
        private float activeTime = 0.4f;

        public override IEnumerator Execute()
        {
            List<Vector2> points =
                new List<Vector2>();

            for (int i = 0; i < starCount; i++)
            {
                points.Add(
                    Arena.RandomPoint()
                );
            }

            List<StarThread> threads =
                new List<StarThread>();

            for (
                int i = 0;
                i < points.Count - 1;
                i++)
            {
                StarThread thread =
                    Instantiate(
                        threadPrefab
                    );

                thread.Setup(
                    points[i],
                    points[i + 1]
                );

                threads.Add(thread);
            }

            yield return Wait(warningTime);

            // 여기서 Zodiac 눈 Blink 애니메이션 실행하면 됨.

            foreach (StarThread thread in threads)
            {
                thread.SetDangerous(true);
            }

            yield return Wait(activeTime);

            foreach (StarThread thread in threads)
            {
                if (thread != null)
                    Destroy(thread.gameObject);
            }
        }
    }
}