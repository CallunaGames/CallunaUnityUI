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

        private ReadonlyObservable<TValue> _observable;
        private CultureInfo _cultureInfo;

        // Cached formatter resolved once in Initialize() to avoid boxing TValue on every
        // value-change event. The naïve `value is IFormattable` pattern-match boxes value
        // types (float, int, double, long) each time FormatDisplayText() is called.
        private Func<TValue, string> _formatter;

        void Injectable.Inject(Resolver resolver)
        {
            _observable = resolver.Resolve<ReadonlyObservable<TValue>>();
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
        }

        private void Reset()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        void Initializable.Initialize()
        {
            // The IFormattable check is done once here against the type, not the value,
            // so no boxing occurs on the hot path inside FormatDisplayText().
            if (typeof(IFormattable).IsAssignableFrom(typeof(TValue)))
                _formatter = v => ((IFormattable)v).ToString(_format, _cultureInfo);
            else
                _formatter = v => v?.ToString() ?? string.Empty;

            _observable.OnChanged += UpdateText;
            UpdateText();
        }

        void Cleanable.Clean()
        {
            _observable.OnChanged -= UpdateText;
        }

        protected virtual void UpdateText()
        {
            _text.text = FormatDisplayText();
        }

        protected virtual string FormatDisplayText()
        {
            return _formatter(_observable.Value);
        }
    }
}
