using System;
using KimLIb.ModuleSystems;
using Member.ODK._01_Script;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public abstract class AbstractDamageCaster : MonoBehaviour
    {
        [field:SerializeField] public DamageDataSO DamageData { get; private set; }
        [SerializeField] private int maxHitCount;
        [SerializeField] protected LayerMask whatIsEnemy;
        [SerializeField] protected ContactFilter2D contactFilter;
        
        public ModuleOwner CasterOwner { get; private set; }
        public Vector2 LastHitPosition { get; protected set; }
        public Vector2 LastHitNormal { get; protected set; }
        public bool LastHitCritical { get; protected set; }

        protected Collider2D[] _hitResults;
        public Action<DamageData> OnHit;

        public virtual void InitCaster(ModuleOwner owner)
        {
            CasterOwner = owner;
            _hitResults = new Collider2D[maxHitCount];
        }

        public abstract bool CastDamage(Vector2 position, Vector2 direction);
    }
}