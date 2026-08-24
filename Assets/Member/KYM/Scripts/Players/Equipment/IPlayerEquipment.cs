using UnityEngine;

namespace Member.KYM.Scripts.Players.Equipment
{
    public interface IPlayerEquipment
    {
        EquipmentDefinitionSO Definition { get; }
        GameObject EquipmentObject { get; }
        bool IsEquipped { get; }

        void Initialize(PlayerController player);
        void Equip();
        void Unequip();
    }
}
