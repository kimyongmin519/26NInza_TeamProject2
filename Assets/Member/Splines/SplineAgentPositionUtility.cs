using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public static class SplineAgentPositionUtility
    {
        public static float GetBodyToFeetOffsetY(Rigidbody2D rigidBody)
        {
            if (rigidBody == null)
                return 0f;

            Collider2D bodyCollider = rigidBody.GetComponent<Collider2D>();
            return bodyCollider != null
                ? rigidBody.position.y - bodyCollider.bounds.min.y
                : 0f;
        }

        public static Vector2 GetFeetPosition(
            Rigidbody2D rigidBody,
            float bodyToFeetOffsetY)
        {
            if (rigidBody == null)
                return Vector2.zero;

            return rigidBody.position - Vector2.up * bodyToFeetOffsetY;
        }

        public static Vector2 ToBodyPosition(
            Vector2 feetPosition,
            float bodyToFeetOffsetY)
        {
            return feetPosition + Vector2.up * bodyToFeetOffsetY;
        }
    }
}
