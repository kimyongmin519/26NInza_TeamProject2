using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Member.Wst.Scripts.Achievements.Conditions;
using Member.Wst.Scripts.Achievements.Datas;
using Member.Wst.Scripts.Achievements.Saves;
using Member.Wst.Scripts.CoreSystems.SaveCode;

namespace Member.Wst.Scripts.Achievements
{
    /// <summary>
    /// Inspector에 넣은 업적들을 Bind 해주고, 진행도를 로드/저장하는 클래스
    /// </summary>
    public class AchievementDataManager : MonoBehaviour
    {
        [field: SerializeField] public List<AchievementData> Achievements { get; private set; }
        [field: SerializeField] public SaveFileNameSO SaveFileName { get; private set; }

        private Dictionary<AchievementType, AchievementData> _achievementDatasDict;
        private readonly List<IAchievementCondition> _boundConditions = new();
        private JsonSaveService _jsonSaveService;

        private void Awake()
        {
            _jsonSaveService = new JsonSaveService(SaveFileName);
            InitSaveData();
            _achievementDatasDict = Achievements.ToDictionary(data => data.AchievementDataSO.AchievementType);
            LoadSaveData();
        }

        private void Start()
        {
            foreach (AchievementData data in Achievements)
            {
                AchievementConditionSO conditionSo = data?.AchievementDataSO?.Condition;
                if (conditionSo == null)
                    continue;
                
                IAchievementCondition condition = conditionSo.Create();
                condition.Bind(data);
                _boundConditions.Add(condition);
            }
        }

        private void OnDestroy()
        {
            foreach (IAchievementCondition condition in _boundConditions)
                condition.Unbind();

            _boundConditions.Clear();

            foreach (AchievementData data in Achievements)
                data.OnChanged -= HandleChanged;
        }

        private void LoadSaveData()
        {
            if (!File.Exists(SaveFileName.SavePath))
            {
                Debug.Log("now file not exist");
                return;
            }

            AchieveSaveDataList jsonData = _jsonSaveService.Load<AchieveSaveDataList>();
            if (jsonData == null || jsonData.achieveSaveDatas == null)
            {
                Debug.LogWarning("Save data is empty or invalid.");
                return;
            }

            foreach (AchieveSaveData achieveDataSave in jsonData.achieveSaveDatas)
            {
                if (_achievementDatasDict.TryGetValue(achieveDataSave.achievementType, out AchievementData achievementData))
                    achievementData.ChangeAchieveData(achieveDataSave);
            }
        }

        private void InitSaveData()
        {
            foreach (AchievementData data in Achievements)
            {
                data.AchieveSaveData.achievementType = data.AchievementDataSO.AchievementType;
                data.OnChanged += HandleChanged;
            }
        }

        private void HandleChanged(AchievementData data)
        {
            AchieveSaveDataList achieveDatas = new();
            foreach (AchievementData achieveDataSave in Achievements)
                achieveDatas.achieveSaveDatas.Add(achieveDataSave.AchieveSaveData);

            _jsonSaveService.Save(achieveDatas);
        }
    }
}
