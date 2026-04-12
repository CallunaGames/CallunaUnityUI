using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// Populates the grid at startup and exposes buttons to add and remove items at runtime,
    /// demonstrating that the virtual scroll responds live to list changes.
    /// </summary>
    public class GridTester : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _removeButton;
        [SerializeField] private int    _initialItemCount = 200;

        private ObservableList<ItemData> _items;

        void Injectable.Inject(Resolver resolver)
        {
            _items = resolver.Resolve<ObservableList<ItemData>>();
        }

        void Initializable.Initialize()
        {
            _addButton.onClick.AddListener(AddItem);
            _removeButton.onClick.AddListener(RemoveLastItem);

            for (int i = 0; i < _initialItemCount; i++)
                _items.Add(CreateItem(i));
        }

        void Cleanable.Clean()
        {
            _addButton.onClick.RemoveListener(AddItem);
            _removeButton.onClick.RemoveListener(RemoveLastItem);
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

        private static ItemData CreateItem(int index) => new ItemData
        {
            Index = index,
            Label = $"Item {index}",
        };
    }
}
