using System;
using System.Collections.Generic;

namespace Member.KYM.Scripts.Players.Equipment
{
    [Serializable]
    public class PlayerEquipmentSaveData
    {
        public List<string> unlockedEquipmentIds = new();
        public List<string> equippedEquipmentIds = new();
    }
}
