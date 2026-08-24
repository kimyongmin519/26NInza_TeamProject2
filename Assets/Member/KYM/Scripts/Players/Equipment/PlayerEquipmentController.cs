using System;
using System.Collections.Generic;
using System.Linq;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.Equipment
{
    public class PlayerEquipmentController : MonoBehaviour, IModule
    {
        [SerializeField] private Transform equipmentRoot;
        [SerializeField] private EquipmentDefinitionSO[] equipmentCatalog;
        [SerializeField] private EquipmentDefinitionSO[] initiallyUnlocked;
        [SerializeField] private EquipmentDefinitionSO[] initiallyEquipped;

        public event Action<EquipmentDefinitionSO> EquipmentUnlocked;
        public event Action<EquipmentDefinitionSO> EquipmentEquipped;
        public event Action<EquipmentDefinitionSO> EquipmentUnequipped;

        private readonly Dictionary<string, EquipmentDefinitionSO>
            _definitions = new();

        private readonly Dictionary<string, IPlayerEquipment>
            _runtimeEquipment = new();

        private readonly Dictionary<EquipmentSlot, IPlayerEquipment>
            _equippedBySlot = new();

        private readonly HashSet<string> _unlockedEquipmentIds = new();

        private PlayerController _player;

        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null);

            if (equipmentRoot == null)
                equipmentRoot = transform;

            BuildDefinitionMap();
            RegisterPreplacedEquipment();

            foreach (IPlayerEquipment equipment in _runtimeEquipment.Values)
                equipment.Unequip();

            foreach (EquipmentDefinitionSO definition in
                     initiallyUnlocked ?? Array.Empty<EquipmentDefinitionSO>())
                Unlock(definition);

            foreach (EquipmentDefinitionSO definition in
                     initiallyEquipped ?? Array.Empty<EquipmentDefinitionSO>())
                UnlockAndEquip(definition);
        }

        public bool Unlock(EquipmentDefinitionSO definition)
        {
            if (!IsValid(definition))
                return false;

            _definitions[definition.EquipmentId] = definition;

            if (!_unlockedEquipmentIds.Add(definition.EquipmentId))
                return false;

            EquipmentUnlocked?.Invoke(definition);
            return true;
        }

        public bool Unlock(string equipmentId)
        {
            return TryGetDefinition(equipmentId, out EquipmentDefinitionSO definition)
                && Unlock(definition);
        }

        public bool UnlockAndEquip(EquipmentDefinitionSO definition)
        {
            if (!IsValid(definition))
                return false;

            Unlock(definition);
            return Equip(definition);
        }

        public bool UnlockAndEquip(string equipmentId)
        {
            return TryGetDefinition(equipmentId, out EquipmentDefinitionSO definition)
                && UnlockAndEquip(definition);
        }

        public bool Equip(EquipmentDefinitionSO definition)
        {
            if (!IsValid(definition) || !IsUnlocked(definition.EquipmentId))
                return false;

            IPlayerEquipment equipment = GetOrCreateEquipment(definition);
            if (equipment == null)
                return false;

            if (_equippedBySlot.TryGetValue(
                    definition.Slot,
                    out IPlayerEquipment current
                ))
            {
                if (current == equipment && current.IsEquipped)
                    return true;

                UnequipInternal(current);
            }

            equipment.Equip();
            _equippedBySlot[definition.Slot] = equipment;
            EquipmentEquipped?.Invoke(definition);
            return true;
        }

        public bool Equip(string equipmentId)
        {
            return TryGetDefinition(equipmentId, out EquipmentDefinitionSO definition)
                && Equip(definition);
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (!_equippedBySlot.TryGetValue(slot, out IPlayerEquipment equipment))
                return false;

            UnequipInternal(equipment);
            return true;
        }

        public bool Unequip(string equipmentId)
        {
            IPlayerEquipment equipment = _equippedBySlot.Values
                .FirstOrDefault(item =>
                    item.Definition != null &&
                    item.Definition.EquipmentId == equipmentId
                );

            if (equipment == null)
                return false;

            UnequipInternal(equipment);
            return true;
        }

        public bool IsUnlocked(string equipmentId)
        {
            return !string.IsNullOrWhiteSpace(equipmentId) &&
                   _unlockedEquipmentIds.Contains(equipmentId);
        }

        public bool IsEquipped(string equipmentId)
        {
            return _equippedBySlot.Values.Any(item =>
                item.IsEquipped &&
                item.Definition != null &&
                item.Definition.EquipmentId == equipmentId
            );
        }

        public PlayerEquipmentSaveData CaptureSaveData()
        {
            return new PlayerEquipmentSaveData
            {
                unlockedEquipmentIds = _unlockedEquipmentIds.ToList(),
                equippedEquipmentIds = _equippedBySlot.Values
                    .Where(item => item.IsEquipped && item.Definition != null)
                    .Select(item => item.Definition.EquipmentId)
                    .ToList()
            };
        }

        public void RestoreSaveData(PlayerEquipmentSaveData data)
        {
            if (data == null)
                return;

            foreach (IPlayerEquipment equipment in _equippedBySlot.Values.ToArray())
                UnequipInternal(equipment);

            _unlockedEquipmentIds.Clear();

            foreach (string equipmentId in
                     data.unlockedEquipmentIds ?? new List<string>())
                Unlock(equipmentId);

            foreach (string equipmentId in
                     data.equippedEquipmentIds ?? new List<string>())
                Equip(equipmentId);
        }

        private void BuildDefinitionMap()
        {
            _definitions.Clear();

            foreach (EquipmentDefinitionSO definition in
                     equipmentCatalog ?? Array.Empty<EquipmentDefinitionSO>())
            {
                if (IsValid(definition))
                    _definitions[definition.EquipmentId] = definition;
            }
        }

        private void RegisterPreplacedEquipment()
        {
            IPlayerEquipment[] equipmentItems = GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IPlayerEquipment>()
                .ToArray();

            foreach (IPlayerEquipment equipment in equipmentItems)
                RegisterEquipment(equipment);
        }

        private void RegisterEquipment(IPlayerEquipment equipment)
        {
            if (equipment?.Definition == null ||
                !IsValid(equipment.Definition))
            {
                return;
            }

            equipment.Initialize(_player);
            _runtimeEquipment[equipment.Definition.EquipmentId] = equipment;
            _definitions[equipment.Definition.EquipmentId] = equipment.Definition;
        }

        private IPlayerEquipment GetOrCreateEquipment(
            EquipmentDefinitionSO definition
        )
        {
            if (_runtimeEquipment.TryGetValue(
                    definition.EquipmentId,
                    out IPlayerEquipment equipment
                ))
            {
                return equipment;
            }

            if (definition.Prefab == null)
                return null;

            GameObject instance = Instantiate(
                definition.Prefab,
                equipmentRoot
            );

            equipment = instance
                .GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IPlayerEquipment>()
                .FirstOrDefault();

            if (equipment == null)
            {
                Destroy(instance);
                return null;
            }

            RegisterEquipment(equipment);
            equipment.Unequip();
            return equipment;
        }

        private void UnequipInternal(IPlayerEquipment equipment)
        {
            if (equipment == null || equipment.Definition == null)
                return;

            EquipmentDefinitionSO definition = equipment.Definition;
            equipment.Unequip();
            _equippedBySlot.Remove(definition.Slot);
            EquipmentUnequipped?.Invoke(definition);
        }

        private bool TryGetDefinition(
            string equipmentId,
            out EquipmentDefinitionSO definition
        )
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(equipmentId) &&
                   _definitions.TryGetValue(equipmentId, out definition);
        }

        private static bool IsValid(EquipmentDefinitionSO definition)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(definition.EquipmentId);
        }
    }
}
