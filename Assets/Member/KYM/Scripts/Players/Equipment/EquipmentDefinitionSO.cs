using UnityEngine;

namespace Member.KYM.Scripts.Players.Equipment
{
    public enum EquipmentSlot
    {
        Arm,
        Head,
        Body,
        Utility
    }

    [CreateAssetMenu(
        fileName = "Equipment data",
        menuName = "Player/Equipment Definition"
    )]
    public class EquipmentDefinitionSO : ScriptableObject
    {
        [SerializeField] private string equipmentId;
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private GameObject prefab;

        public string EquipmentId => equipmentId;
        public EquipmentSlot Slot => slot;
        public GameObject Prefab => prefab;

        private void OnValidate()
        {
            equipmentId = equipmentId?.Trim();
        }
    }
}
