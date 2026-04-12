using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI.Samples.VirtualScrollUI
{
    /// <summary>
    /// A single grid cell. Extends <see cref="VirtualScrollItem{TData}"/> so the pool
    /// delivers <see cref="ItemData"/> via DI argument injection on every activation.
    /// </summary>
    public class ItemCell : VirtualScrollItem<ItemData>
    {
        [SerializeField] private TextMeshProUGUI _indexLabel;
        [SerializeField] private TextMeshProUGUI _contentLabel;
        [SerializeField] private Image           _background;

        private static readonly Color[] _rowColors =
        {
            new Color(0.20f, 0.20f, 0.22f),
            new Color(0.16f, 0.16f, 0.18f),
        };

        protected override void OnInitialize()
        {
            _indexLabel.text   = $"#{Data.Index:000}";
            _contentLabel.text = Data.Label;
            _background.color  = _rowColors[Data.Index % _rowColors.Length];
        }

        protected override void OnClean()
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
