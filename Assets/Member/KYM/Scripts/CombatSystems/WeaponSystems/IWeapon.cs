using System;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.WeaponSystems
{
    public interface IWeapon
    {
        event Action OnAttackEnd;

        int WeaponIndex { get; }
        bool IsEquipped { get; }
        bool IsAttacking { get; }

        void InitializeWeapon(IWeaponModule weaponModule);
        void Equip();
        void Unequip();
        void SetTarget(GameObject target);
        bool CanAttack(GameObject target = null);
        void Attack(GameObject target = null);
        void StopAttack();
    }
}
