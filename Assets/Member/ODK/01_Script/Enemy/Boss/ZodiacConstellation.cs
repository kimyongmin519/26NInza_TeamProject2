using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacConstellation : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private bool isBroken;

        public bool IsBroken => isBroken;

        public void Break()
        {
            if (isBroken)
                return;

            isBroken = true;

            if (animator != null)
            {
                animator.SetTrigger("Break");
            }
            else
            {

                gameObject.SetActive(false);
            }
        }
    }
}