using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGMLib.ObjectPool.Editor
{
    [CustomEditor(typeof(PoolItemSO))]
    public class PoolItemSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset viewAsset = default;

        private TextField _nameField;
        private Button _changeBtn;
        private ObjectField _prefabField;
        
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            viewAsset.CloneTree(root);

            _nameField = root.Q<TextField>("item-name");
            _changeBtn = root.Q<Button>("change-btn");
            _prefabField = root.Q<ObjectField>("prefab-field");
            
            _changeBtn.clicked += HandleChangeBtnClick;
            _nameField.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            _prefabField.RegisterValueChangedCallback(HandlePrefabChangeEvent);
            
            return root;
        }

        private void HandlePrefabChangeEvent(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null) return;
            GameObject newPrefab = evt.newValue as GameObject;
            Debug.Assert(newPrefab != null, "게임오브젝트만 할당가능합니다.");
            PoolItemSO item = target as PoolItemSO; //현재 SO를 
            //풀링이 불가능한 프리팹을 넣어버린거지.
            if (!newPrefab.TryGetComponent(out IPoolable poolable))
            {
                item.prefab = null;
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Error", "IPoolable 콤포넌트가 붙은 프리팹이 필요합니다.", "OK");
                return;
            }
            
            poolable.PoolItem = item;
            EditorUtility.SetDirty(newPrefab);
            AssetDatabase.SaveAssets();
        }

        private void HandleChangeBtnClick()
        {
            string newName = _nameField.text;
            if (string.IsNullOrEmpty(newName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a name.", "OK");
                return;
            }
            
            string assetPath = AssetDatabase.GetAssetPath(target);
            string message = AssetDatabase.RenameAsset(assetPath, newName);
            if (string.IsNullOrEmpty(message))
            {
                target.name = newName;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", message, "OK");
            }
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if(evt.keyCode == KeyCode.Return)
                HandleChangeBtnClick();
        }
    }
}