using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Players;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public abstract class AbstractDamageCaster : MonoBehaviour
    {
        [field:SerializeField] public DamageDataSO DamageData { get; private set; }
        [SerializeField] private int maxHitCount;
        [SerializeField] protected ContactFilter2D contactFilter;
        
        public ModuleOwner CasterOwner { get; private set; }
        public Vector2 LastHitPosition { get; protected set; }
        public Vector2 LastHitNormal { get; protected set; }
        public bool LastHitCritical { get; protected set; }

        protected Collider2D[] _hitResults;
        public Action<DamageData> OnHit;
        public UnityEvent OnHitOwnerPlayer;

        public virtual void InitCaster(ModuleOwner owner)
        {
            CasterOwner = owner;
            _hitResults = new Collider2D[maxHitCount];
        }

        protected bool IsCasterOwner(Collider2D hitCollider)
        {
            if (CasterOwner == null || hitCollider == null)
                return false;

            ModuleOwner hitOwner =
                hitCollider.GetComponentInParent<ModuleOwner>();
            return ReferenceEquals(hitOwner, CasterOwner);
        }

        protected bool TryApplyDamage(Collider2D hitCollider, Vector2 hitPoint, Vector2 hitNormal)
        {
            if (hitCollider == null || IsCasterOwner(hitCollider))
                return false;
            
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return false;

            float baseDamage = DamageData.BaseDamageAmount;
            Vector2 knockbackForce = ResolveKnockbackForce(hitCollider, hitNormal);
            DamageData damageData = new DamageData
            {
                Amount = CasterOwner is PlayerController
                    ? baseDamage
                    : DamageConverter.DamageToPlayerHitDamage(baseDamage),
                DamageType = DamageData.DamageType,
                CriticalType = CriticalType.Normal,
                KnockbackForce = knockbackForce
            };

            LastHitPosition = hitPoint;
            LastHitNormal = hitNormal;
            LastHitCritical = false;

            damageable.TakeDamage(damageData);
            OnHit?.Invoke(damageData);
            if (CasterOwner is PlayerController)
            {
                OnHitOwnerPlayer?.Invoke();
            }
            return true;
        }

        private Vector2 ResolveKnockbackForce(Collider2D hitCollider, Vector2 hitNormal)
        {
            Vector2 configuredForce = DamageData.KnockbackForce;
            if (Mathf.Approximately(configuredForce.x, 0f))
                return configuredForce;

            float horizontalDirection = CasterOwner == null
                ? 0f
                : hitCollider.bounds.center.x - CasterOwner.transform.position.x;

            if (Mathf.Approximately(horizontalDirection, 0f))
                horizontalDirection = -hitNormal.x;

            if (Mathf.Approximately(horizontalDirection, 0f))
                horizontalDirection = 1f;

            configuredForce.x = Mathf.Abs(configuredForce.x) * Mathf.Sign(horizontalDirection);
            return configuredForce;
        }

        public virtual bool CastDamage(Vector2 position, Vector2 direction)
        {
            return false;
        }

        public virtual bool CastDamage(Collider2D hitCollider, Vector2 hitPoint, Vector2 hitNormal)
        {
            return false;
        }
    }
}
