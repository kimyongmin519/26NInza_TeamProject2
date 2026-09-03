using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacRing : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 15f;

        private void Update()
        {
            transform.Rotate(
                0f,
                0f,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}