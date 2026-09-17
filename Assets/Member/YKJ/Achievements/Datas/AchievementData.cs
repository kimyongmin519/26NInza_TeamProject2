using System;
using UnityEngine;
using Member.Wst.Scripts.Achievements.Saves;

namespace Member.Wst.Scripts.Achievements.Datas
{
    [Serializable]
    public class AchievementData
    { 
        [field: SerializeField] public AchievementDataSO AchievementDataSO { get; private set; }
        private AchieveSaveData _saveData;
        public AchieveSaveData AchieveSaveData
        {
            get => _saveData ??= new AchieveSaveData();
            private set => _saveData = value;
        }
        
        public event Action<AchievementData> OnChanged;
        public event Action OnComplete;
        public void AddDegree(int value)
        {
            if (AchieveSaveData.isComplete || value <= 0)
                return;

            int targetValue = Mathf.Max(1, AchievementDataSO.TargetDegree);
            AchieveSaveData.nowAchievementDegree = Mathf.Min(
                AchieveSaveData.nowAchievementDegree + value,
                targetValue);

            if (AchieveSaveData.nowAchievementDegree == targetValue)
                Complete();

            OnChanged?.Invoke(this);
        }

        public void ChangeAchieveData(AchieveSaveData achieveSaveData)
        {
            AchieveSaveData = achieveSaveData;
        }
        
        private void Complete()
        {
            AchieveSaveData.isComplete = true;
            OnComplete?.Invoke();
        }
    }
}
