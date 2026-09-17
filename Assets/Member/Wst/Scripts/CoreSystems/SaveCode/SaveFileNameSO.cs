using System.IO;
using UnityEngine;

namespace Member.Wst.Scripts.CoreSystems.SaveCode
{
    [CreateAssetMenu(fileName = "SavePath", menuName = "SO/Save/SavePath", order = 0)]
    public class SaveFileNameSO : ScriptableObject
    {
        [SerializeField] private string savePath;
        private const string Extension = ".json";
        private const string SaveFolder = "Saves";
        
        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFolder, savePath + Extension);
    }
}