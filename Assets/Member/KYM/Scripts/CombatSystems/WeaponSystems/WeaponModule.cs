 using System;
using System.Collections.Generic;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.WeaponSystems
{
    public class WeaponModule : MonoBehaviour, IModule, IWeaponModule
    {
        public ModuleOwner Owner { get; private set; }
        public IWeapon EquippedWeapon { get; private set; }
        public GameObject Target { get; private set; }

        public event Action OnCurrentWeaponAttackEnd;

        private readonly Dictionary<int, IWeapon> _weapons = new();

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            _weapons.Clear();

            foreach (IWeapon weapon in GetComponentsInChildren<IWeapon>(true))
            {
                if (!_weapons.TryAdd(weapon.WeaponIndex, weapon))
                {
                    Debug.LogError(
                        $"{name}에 WeaponIndex {weapon.WeaponIndex}가 중복되었습니다.",
                        this);
                    continue;
                }

                weapon.InitializeWeapon(this);
            }
        }

        public bool TryEquipWeapon(int weaponIndex)
        {
            IWeapon weapon = GetWeapon(weaponIndex);
            if (weapon == null)
                return false;

            if (ReferenceEquals(EquippedWeapon, weapon))
                return true;

            SetEquippedWeapon(weapon);
            return true;
        }

        public void UnequipCurrentWeapon()
        {
            SetEquippedWeapon(null);
        }

        public void SetTarget(GameObject target)
        {
            Target = target;

            foreach (IWeapon weapon in _weapons.Values)
                weapon.SetTarget(target);
        }

        public bool CanAttack(GameObject target = null)
        {
            return EquippedWeapon != null && EquippedWeapon.CanAttack(target);
        }

        public bool TryAttack(GameObject target = null)
        {
            if (target != null)
                SetTarget(target);

            GameObject attackTarget = target != null ? target : Target;
            if (!CanAttack(attackTarget))
                return false;

            EquippedWeapon.Attack(attackTarget);
            return true;
        }

        public void StopCurrentAttack()
        {
            EquippedWeapon?.StopAttack();
        }

        public IWeapon GetWeapon(int weaponIndex)
        {
            _weapons.TryGetValue(weaponIndex, out IWeapon weapon);
            return weapon;
        }

        private void SetEquippedWeapon(IWeapon weapon)
        {
            if (EquippedWeapon != null)
            {
                EquippedWeapon.Unequip();
                EquippedWeapon.OnAttackEnd -= HandleCurrentWeaponAttackEnd;
            }

            EquippedWeapon = weapon;

            if (EquippedWeapon != null)
            {
                EquippedWeapon.SetTarget(Target);
                EquippedWeapon.Equip();
                EquippedWeapon.OnAttackEnd += HandleCurrentWeaponAttackEnd;
            }
        }

        private void HandleCurrentWeaponAttackEnd()
        {
            OnCurrentWeaponAttackEnd?.Invoke();
        }

        private void OnDestroy()
        {
            if (EquippedWeapon != null)
                EquippedWeapon.OnAttackEnd -= HandleCurrentWeaponAttackEnd;
        }
    }
}
