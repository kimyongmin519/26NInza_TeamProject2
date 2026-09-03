using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class AriesPattern : ZodiacPattern
    {
        [SerializeField]
        private ZodiacVerticalBeam beamPrefab;

        [SerializeField]
        private int randomBeamCount = 4;

        [SerializeField]
        private float telegraphTime = 0.7f;

        [SerializeField]
        private float beamDuration = 0.35f;

        [SerializeField]
        private int repeatCount = 3;

        public override IEnumerator Execute()
        {
            for (int r = 0; r < repeatCount; r++)
            {
                foreach (Transform player in Players)
                {
                    SpawnBeam(player.position);
                }

                for (
                    int i = 0;
                    i < randomBeamCount;
                    i++)
                {
                    SpawnBeam(Arena.RandomPoint());
                }

                yield return Wait(
                    telegraphTime + beamDuration
                );

                yield return Wait(0.25f);
            }
        }

        private void SpawnBeam(
            Vector2 position)
        {
            ZodiacVerticalBeam beam =
                Instantiate(
                    beamPrefab,
                    new Vector2(
                        position.x,
                        Arena.MinY
                    ),
                    Quaternion.identity
                );

            beam.Activate(
                telegraphTime / Speed,
                beamDuration / Speed
            );
        }
    }
}