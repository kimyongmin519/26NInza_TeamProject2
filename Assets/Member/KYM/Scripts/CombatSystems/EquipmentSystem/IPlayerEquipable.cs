using Member.KYM.Scripts.Players;

namespace Member.KYM.Scripts.CombatSystems.EquipmentSystem
{
    public interface IPlayerEquipable
    {
        bool IsEquipped { get; }

        void Initialize(PlayerController player);
        void Equip();
        void Unequip();
    }
}