using Calluna.DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Populates the grid at startup and exposes buttons to add and remove items at runtime,
    /// demonstrating that the virtual scroll responds live to list changes.
    /// </summary>
    public class ScrollViewTester : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private ItemGrid _grid;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _removeButton;
        [SerializeField] private Button _scrollToButton;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private int _initialItemCount = 200;
        [SerializeField] private ScrollAlignment _alignment = ScrollAlignment.Center;
        [SerializeField] private float _scrollDuration = 0.5f;

        private ObservableList<ItemData> _items;

        void Injectable.Inject(Resolver resolver)
        {
            _items = resolver.Resolve<ObservableList<ItemData>>();
        }

        void Initializable.Initialize()
        {
            _addButton.onClick.AddListener(AddItem);
            _removeButton.onClick.AddListener(RemoveLastItem);
            _scrollToButton.onClick.AddListener(ScrollToInputIndex);

            for (int i = 0; i < _initialItemCount; i++)
                _items.Add(CreateItem(i));
        }

        void Cleanable.Clean()
        {
            _addButton.onClick.RemoveListener(AddItem);
            _removeButton.onClick.RemoveListener(RemoveLastItem);
            _scrollToButton.onClick.RemoveListener(ScrollToInputIndex);
        }

        private void AddItem()
        {
            _items.Add(CreateItem(_items.Count));
        }

        private void RemoveLastItem()
        {
            if (_items.Count > 0)
                _items.RemoveAt(_items.Count - 1);
        }

        private void ScrollToInputIndex()
        {
            if (!int.TryParse(_inputField.text, out int index)) return;
            if (_items.Count == 0 || index < 0 || index >= _items.Count) return;
            _grid.ScrollToIndex(index, _alignment, _scrollDuration);
        }

        private static ItemData CreateItem(int index) => new ItemData
        {
            Index = index,
            Label = $"Item {index}",
        };
    }
}