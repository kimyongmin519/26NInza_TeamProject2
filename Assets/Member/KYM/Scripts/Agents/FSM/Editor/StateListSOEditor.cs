using System.IO;
using System.Linq;
using Member.KYM.Scripts.CoreSystems;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Member.KYM.Scripts.Agents.FSM.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            editorView.CloneTree(root);

            root.Q<Button>("GenerateButton").clicked += HandleGenerateEnumClick;
            
            return root;
        }

        private void HandleGenerateEnumClick()
        {
            StateListSO listData = target as StateListSO;
            
            Debug.Assert(listData != null, "Target data is null check editor");
            
            int index = 0;
            string enumString = string.Join(",", listData.states.Select(so =>
            {
                so.stateIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));
            
            string code = string.Format(CodeFormat.EnumFormat,"Member.KYM.Scripts.Agents.FSM", listData.stateEnum, enumString);

            string scriptPath = AssetDatabase.GetAssetPath( MonoScript.FromScriptableObject(this));
            string directoryName = Path.GetDirectoryName(scriptPath);
            Debug.Assert(directoryName != null, "Parent directory is null");
            
            DirectoryInfo parentDirectory = Directory.GetParent(directoryName);
            Debug.Assert(parentDirectory != null, "Parent directory is null");
            
            string path = parentDirectory.FullName;
            File.WriteAllText($"{path}/{listData.stateEnum}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}