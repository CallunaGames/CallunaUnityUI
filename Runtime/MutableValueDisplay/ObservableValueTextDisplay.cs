using System;
using System.Globalization;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class ObservableValueTextDisplay<TValue> : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private string _format = string.Empty;

        private ReadonlyObservable<TValue> _value;
        private CultureInfo _cultureInfo;

        void Injectable.Inject(Resolver resolver)
        {
            _value = resolver.Resolve<ReadonlyObservable<TValue>>();
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
        }

        private void Reset()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        void Initializable.Initialize()
        {
            _value.OnChanged += UpdateText;
            UpdateText();
        }

        void Cleanable.Clean()
        {
            _value.OnChanged -= UpdateText;
        }

        protected virtual void UpdateText()
        {
            _text.text = GetText();
        }

        protected virtual string GetText()
        {
            TValue value = _value.Value;
            if (value is IFormattable formattable)
                return formattable.ToString(_format, _cultureInfo);
            return value != null ? value.ToString() : string.Empty;
        }
    }
}
