using System;
using GGMLib.ObjectPool.Runtime;
using UnityEngine.UIElements;

namespace GGMLib.ObjectPool.Editor
{
    public class PoolItemView
    {
        private Label _nameLabel;
        private Button _deleteButton;
        private VisualElement _rootElement;
        private Label _warningLabel;
        
        public event Action<PoolItemView> OnDelete;
        public event Action<PoolItemView> OnSelect;

        public string Name
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        public PoolItemSO ItemSO { get; private set; }

        public bool IsActive
        {
            get => _rootElement.ClassListContains("active");
            set => _rootElement.EnableInClassList("active", value);
        }

        public bool IsEmpty
        {
            get => _warningLabel.ClassListContains("active");
            set => _warningLabel.EnableInClassList("active", value);
        }

        public PoolItemView(VisualElement rootElement, PoolItemSO item)
        {
            ItemSO = item;
            _rootElement = rootElement.Q("pool-item");
            _nameLabel = _rootElement.Q<Label>("item-name");
            _deleteButton = _rootElement.Q<Button>("delete-btn");
            _warningLabel = _rootElement.Q<Label>("warning-label");
            
            _deleteButton.RegisterCallback<ClickEvent>(evt =>
            {
                OnDelete?.Invoke(this);
                evt.StopPropagation();
            });
            
            _rootElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnSelect?.Invoke(this);
                evt.StopPropagation();
            });
        }
    }
}