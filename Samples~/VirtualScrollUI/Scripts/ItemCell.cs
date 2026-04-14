using Calluna.DI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// A single grid cell. Implements the Calluna DI lifecycle so the pool
    /// delivers <see cref="ItemData"/> via argument injection on every activation.
    /// </summary>
    public class ItemCell : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _indexLabel;
        [SerializeField] private TextMeshProUGUI _contentLabel;
        [SerializeField] private Image           _background;

        private static readonly Color[] _rowColors =
        {
            new Color(0.20f, 0.20f, 0.22f),
            new Color(0.16f, 0.16f, 0.18f),
        };

        private ItemData _data;

        void Injectable.Inject(Resolver resolver)
        {
            _data = resolver.Resolve<ItemData>();
        }

        void Initializable.Initialize()
        {
            _indexLabel.text   = $"#{_data.Index:000}";
            _contentLabel.text = _data.Label;
            _background.color  = _rowColors[_data.Index % _rowColors.Length];
        }

        void Cleanable.Clean()
        {
            _indexLabel.text   = string.Empty;
            _contentLabel.text = string.Empty;
        }

        private void Reset()
        {
            _indexLabel   = transform.Find("IndexLabel")  ?.GetComponent<TextMeshProUGUI>();
            _contentLabel = transform.Find("ContentLabel")?.GetComponent<TextMeshProUGUI>();
            _background   = GetComponent<Image>();
        }
    }
}
