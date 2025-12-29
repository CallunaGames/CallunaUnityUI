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

        public void Inject(Resolver resolver)
        {
            _styleSettings = resolver.Resolve<ReadonlyObservable<ColorStyleSettings>>();
        }

        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
        }

        public void Initialize()
        {
            _initialColor = _graphic.color;
            _styleSettings.OnChanged += UpdateColor;
            UpdateColor();
        }

        public void Clean()
        {
            _styleSettings.OnChanged -= UpdateColor;
        }

        private void UpdateColor()
        {
            _graphic.color = _styleSettings.HasValue ? _styleSettings.Value.GetColorOf(_colorStyle) : _initialColor;
        }
    }
}