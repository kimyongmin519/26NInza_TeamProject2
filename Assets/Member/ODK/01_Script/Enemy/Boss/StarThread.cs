using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Zodiac
{
    public class StarThread : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer line;

        [SerializeField]
        private EdgeCollider2D hitbox;

        public void Setup(
            Vector2 start,
            Vector2 end)
        {
            transform.position = Vector3.zero;

            line.positionCount = 2;

            line.SetPosition(
                0,
                start
            );

            line.SetPosition(
                1,
                end
            );

            hitbox.points =
                new Vector2[]
                {
                    start,
                    end
                };

            hitbox.enabled = false;
        }

        public void SetDangerous(
            bool dangerous)
        {
            hitbox.enabled = dangerous;

            // 색이나 머티리얼 바꾸고 싶으면
            // 여기서 처리
        }
    }
}