using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ApplyColorStyle : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField, Space] private ColorStyleId _colorStyle;

        private ReadonlyObservable<ColorStyleSettings> _styleSettings;
        private Color _initialColor;

        void Injectable.Inject(Resolver resolver)
        {
            _styleSettings = resolver.Resolve<ReadonlyObservable<ColorStyleSettings>>();
        }

        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
        }

        void Initializable.Initialize()
        {
            _initialColor = _graphic.color;
            _styleSettings.OnChanged += UpdateColor;
            UpdateColor();
        }

        void Cleanable.Clean()
        {
            _styleSettings.OnChanged -= UpdateColor;
        }

        private void UpdateColor()
        {
            if (_styleSettings.HasValue && _styleSettings.Value.TryGetColorOf(_colorStyle, out Color color))
                _graphic.color = color;
            else
                _graphic.color = _initialColor;
        }
    }
}