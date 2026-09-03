using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacVerticalBeam : MonoBehaviour
    {
        [SerializeField]
        private GameObject warningVisual;

        [SerializeField]
        private GameObject beamVisual;

        [SerializeField]
        private Collider2D hitbox;

        public void Activate(
            float warningTime,
            float activeTime)
        {
            StartCoroutine(
                BeamRoutine(
                    warningTime,
                    activeTime
                )
            );
        }

        private IEnumerator BeamRoutine(
            float warningTime,
            float activeTime)
        {
            warningVisual.SetActive(true);
            beamVisual.SetActive(false);
            hitbox.enabled = false;

            yield return new WaitForSeconds(
                warningTime
            );

            warningVisual.SetActive(false);
            beamVisual.SetActive(true);
            hitbox.enabled = true;

            yield return new WaitForSeconds(
                activeTime
            );

            Destroy(gameObject);
        }
    }
}