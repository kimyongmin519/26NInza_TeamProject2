using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class ZodiacArena2D : MonoBehaviour
    {
        [SerializeField]
        private Transform bottomLeft;

        [SerializeField]
        private Transform topRight;

        [SerializeField]
        private Transform projectileRoot;

        private ZodiacBossController boss;

        public float MinX => bottomLeft.position.x;
        public float MaxX => topRight.position.x;

        public float MinY => bottomLeft.position.y;
        public float MaxY => topRight.position.y;

        public Transform ProjectileRoot =>
            projectileRoot;

        public void Initialize(
            ZodiacBossController boss)
        {
            this.boss = boss;
        }

        public Vector2 RandomPoint()
        {
            return new Vector2(
                Random.Range(MinX, MaxX),
                Random.Range(MinY, MaxY)
            );
        }

        public Vector2 RandomTopPosition()
        {
            return new Vector2(
                Random.Range(MinX, MaxX),
                MaxY
            );
        }
    }
}