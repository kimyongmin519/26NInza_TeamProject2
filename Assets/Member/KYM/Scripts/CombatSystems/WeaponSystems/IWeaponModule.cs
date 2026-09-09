using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.WeaponSystems
{
    public interface IWeaponModule
    {
        ModuleOwner Owner { get; }
        IWeapon EquippedWeapon { get; }
        GameObject Target { get; }

        event Action OnCurrentWeaponAttackEnd;

        bool TryEquipWeapon(int weaponIndex);
        void UnequipCurrentWeapon();
        void SetTarget(GameObject target);
        bool CanAttack(GameObject target = null);
        bool TryAttack(GameObject target = null);
        void StopCurrentAttack();
        IWeapon GetWeapon(int weaponIndex);
    }
}
