using Member.ODK._01_Script;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public enum CastType
    {
        CIRCLE, BOX
    }
    public class OverlapDamageCaster : AbstractDamageCaster
    {
        [SerializeField] private CastType castType;
        [SerializeField] private float radius;
        [SerializeField] private Vector2 boxSize;
        [SerializeField] private bool radialKnockback;
        
        public void SetCastType(CastType type) => castType = type;
        public void SetRadius(float r) => radius =  r;
        public void SetBoxSize(Vector2 size) => boxSize = size;
        public void SetRadialKnockback(bool value) => radialKnockback = value;

        public override bool CastDamage(Vector2 position, Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            int cnt = castType switch
            {
                CastType.CIRCLE => Physics2D.OverlapCircle(position, radius, contactFilter, _hitResults),
                CastType.BOX => Physics2D.OverlapBox(position, boxSize, angle, contactFilter, _hitResults), 
                _ => 0
            };

            bool damagedTarget = false;

            for (int i = 0; i < cnt; i++)
            {
                if (IsCasterOwner(_hitResults[i]))
                    continue;

                if (_hitResults[i].TryGetComponent(out IDamageable damageable))
                {
                    Vector2 point = _hitResults[i].ClosestPoint(position);

                    DamageData damageData = new DamageData
                    {

                    };
                    
                    damageable.TakeDamage(damageData);
                    damagedTarget = true;
                    MonoBehaviour target = damageable as MonoBehaviour;
                    Debug.Log($"{damageData.Damage} 데미지 입힙: {target.gameObject.name}"); //테스트 완료
                }
            }
            return damagedTarget;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (castType == CastType.CIRCLE)
                Gizmos.DrawWireSphere(transform.position, radius);
            else if (castType == CastType.BOX)
            {
                Matrix4x4 matrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
                Gizmos.matrix = matrix;
            }
        }
    }
}
