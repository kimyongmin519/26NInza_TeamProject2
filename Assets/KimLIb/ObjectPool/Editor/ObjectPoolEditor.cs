using System;
using System.Collections.Generic;
using System.IO;
using GGMLib.ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGMLib.ObjectPool.Editor
{
    public class ObjectPoolEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset = default;
        [SerializeField] private PoolManagerSO poolManagerAsset = default;
        [SerializeField] private VisualTreeAsset itemAsset = default;

        private string _rootFolder;
        
        private Button _createButton;
        private ScrollView _itemView;

        private List<PoolItemView> _itemList; //데이터 바인딩한 아이템들의 리스트
        private PoolItemView _currentItem; //현재 선택된 아이템
        
        private UnityEditor.Editor _cachedEditor; //재활용을 위한 캐싱 에디터
        private VisualElement _inspector;

        [MenuItem("Tools/ObjectPoolEditor")]
        public static void ShowWindow()
        {
            ObjectPoolEditor wnd = GetWindow<ObjectPoolEditor>();
            wnd.titleContent = new GUIContent("ObjectPoolEditor");
        }

        private string GetCurrentDirectory()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            return Path.GetDirectoryName(scriptPath); //현재 경로를 가져온다. 
        }

        private void InitializeRootFolder()
        {
            string dirName = GetCurrentDirectory(); //현재 경로 알아내고
            DirectoryInfo parentDir = Directory.GetParent(dirName);
            Debug.Assert(parentDir != null, $"부모 경로가 없습니다. 경로 체크 필요 : {dirName}");
            
            string dataPath = Application.dataPath;
            _rootFolder = parentDir.FullName.Replace('\\', '/');
            if (_rootFolder.StartsWith(dataPath))
            {
                _rootFolder = "Assets" + _rootFolder.Substring(dataPath.Length); //프로젝트 상대경로로 변경해준다.
            }
        }

        public void CreateGUI()
        {
            InitializeRootFolder(); //루트폴더를 찾아온다. 
            VisualElement root = rootVisualElement;

            if (visualTreeAsset == null)
            {
                string dirName = GetCurrentDirectory();
                visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/ObjectPoolEditor.uxml");
                Debug.Assert(visualTreeAsset != null, "패키지 폴더에 Uxml이 누락되었습니다. : ObjectPoolEditor.uxml");
            }
            visualTreeAsset.CloneTree(root);

            InitializeItems(root);
            GenerateItemUI();
        }

        private void InitializeItems(VisualElement root)
        {
            _createButton = root.Q<Button>("create-btn");
            _createButton.clicked += HandleCreateItem;
            _itemView = root.Q<ScrollView>("item-view");
            
            _itemView.Clear();
            _itemList = new List<PoolItemView>();

            _inspector = root.Q<VisualElement>("inspector-view");
        }

        private void GenerateItemUI()
        {
            _itemView.Clear();
            _itemList.Clear();
            _inspector.Clear(); //아이템 그리기 전에 전부 클리어 시키고

            //풀매니저 에셋이 없다면 로드한다.
            if (poolManagerAsset == null)
            {
                string filePath = $"{_rootFolder}/PoolManager.asset";
                poolManagerAsset = AssetDatabase.LoadAssetAtPath<PoolManagerSO>(filePath);
                if (poolManagerAsset == null)
                {
                    //찾았는데 없다면 warning띄우고 만들어주기
                    Debug.LogWarning("풀 매니저 에셋이 없어 새로 만듭니다.");
                    poolManagerAsset = ScriptableObject.CreateInstance<PoolManagerSO>();
                    AssetDatabase.CreateAsset(poolManagerAsset, filePath);
                }
            }

            if (itemAsset == null)
            {
                string dirName = GetCurrentDirectory();
                itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/PoolItemView.uxml");
                Debug.Assert(itemAsset != null, "아이템을 표기하기 위한 Uxml이 없습니다. : PoolItemView.uxml");
            }

            foreach (PoolItemSO item in poolManagerAsset.itemList)
            {
                TemplateContainer itemUI = itemAsset.Instantiate();
                PoolItemView bindItemView = new PoolItemView(itemUI, item); //바인드 시킨 클래스를 가져온다.
                _itemList.Add(bindItemView); //리스트에는 바인드된 객체를 넣는다.
                _itemView.Add(itemUI); //리스트 뷰에는 UI를 넣고

                bindItemView.Name = item.itemName;
                bindItemView.IsEmpty = item.prefab == null;
                bindItemView.IsActive = false;

                bindItemView.OnSelect += HandleSelectItem;
                bindItemView.OnDelete += HandleDeleteItem;
            }
        }

        private void HandleSelectItem(PoolItemView itemView)
        {
            if (_currentItem != null)
            {
                _currentItem.IsActive = false; //데이터바인딩을 해놨기때문에 코드를 변경하면 알아서 UI가 변경돼.
            }
            _currentItem = itemView;
            _inspector.Clear();
            UnityEditor.Editor.CreateCachedEditor(_currentItem.ItemSO, null, ref _cachedEditor);
            VisualElement inspectorElement = _cachedEditor.CreateInspectorGUI(); //이건 에디터에서 UXML뷰로 뽑아주는거.

            SerializedObject so = new SerializedObject(_currentItem.ItemSO);
            inspectorElement.Bind(so);
            
            inspectorElement.TrackSerializedObjectValue(so, targetSo =>
            {
                itemView.Name = targetSo.FindProperty("itemName").stringValue;
                itemView.IsEmpty = targetSo.FindProperty("prefab").objectReferenceValue == null;
            });
            
            _inspector.Add(inspectorElement);
        }

        private void HandleDeleteItem(PoolItemView itemView)
        {
            poolManagerAsset.itemList.Remove(itemView.ItemSO);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itemView.ItemSO));
            EditorUtility.SetDirty(poolManagerAsset);
            AssetDatabase.SaveAssets();

            if (_currentItem == itemView)
            {
                _currentItem = null;
                _inspector.Clear();
            }
            
            GenerateItemUI(); 
        }

        private void HandleCreateItem()
        {
            Guid itemGuid = Guid.NewGuid();
            PoolItemSO newItem = ScriptableObject.CreateInstance<PoolItemSO>();
            newItem.itemName = itemGuid.ToString();

            string itemPath = $"{_rootFolder}/Items";
            if (!Directory.Exists(itemPath))
            {
                Directory.CreateDirectory(itemPath);
            }
            
            AssetDatabase.CreateAsset(newItem, $"{itemPath}/{newItem.itemName}.asset");
            
            poolManagerAsset.itemList.Add(newItem);
            EditorUtility.SetDirty(poolManagerAsset);
            AssetDatabase.SaveAssets();
            
            GenerateItemUI();
        }
    }
}
