using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.WeaponSystems
{
    public abstract class AbstractWeapon : MonoBehaviour, IWeapon
    {
        [field: SerializeField, Min(0)] public int WeaponIndex { get; private set; }

        public bool IsEquipped { get; private set; }
        public bool IsAttacking { get; private set; }
        public event Action OnAttackEnd;

        protected IWeaponModule WeaponModule { get; private set; }
        protected ModuleOwner Owner => WeaponModule?.Owner;
        protected GameObject Target { get; private set; }

        public virtual void InitializeWeapon(IWeaponModule weaponModule)
        {
            WeaponModule = weaponModule;
            Target = weaponModule.Target;
            IsEquipped = false;
            IsAttacking = false;
            gameObject.SetActive(false);
        }

        public virtual void Equip()
        {
            gameObject.SetActive(true);
            IsEquipped = true;
        }

        public virtual void Unequip()
        {
            StopAttack();
            IsEquipped = false;
            gameObject.SetActive(false);
        }

        public virtual void SetTarget(GameObject target)
        {
            Target = target;
        }

        public virtual bool CanAttack(GameObject target = null)
        {
            return IsEquipped && !IsAttacking;
        }

        public virtual void Attack(GameObject target = null)
        {
            if (target != null)
                SetTarget(target);

            IsAttacking = true;
        }

        public virtual void StopAttack()
        {
            if (!IsAttacking)
                return;

            IsAttacking = false;
            OnAttackEnd?.Invoke();
        }
    }
}
