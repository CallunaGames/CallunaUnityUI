using System;
using System.Globalization;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class TextInput<TValue> : BasicInput<TValue>
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private string _format = string.Empty;
        [SerializeField] private bool _isEmptyProhibited;
        [SerializeField] private TextInputUpdateMode _updateMode = TextInputUpdateMode.OnValueChanged;

        public event Action<string> ParsingFailed;

        private CultureInfo _cultureInfo;
        private TextInputVisualArgs _visualArgs;
        private Color _originalColor;
        private TValue _lastValidValue;

        // Cached formatter resolved once in OnInitialize() to avoid boxing TValue on every
        // value-change event. The naïve `value is IFormattable` pattern-match boxes value
        // types (float, int) each time FormatValue() is called.
        private Func<TValue, string> _formatter;

        protected override void OnInject(Resolver resolver)
        {
            base.OnInject(resolver);
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
            _visualArgs = resolver.ResolveOptional<TextInputVisualArgs>();
        }

        protected override void OnInitialize()
        {
            // Must be set before base.OnInitialize() because base calls UpdateInput() → FormatValue().
            if (typeof(IFormattable).IsAssignableFrom(typeof(TValue)))
                _formatter = v => ((IFormattable)v).ToString(_format, _cultureInfo);
            else
                _formatter = v => v?.ToString();

            base.OnInitialize();
            if (_visualArgs != null)
                _originalColor = _visualArgs.Target.color;
        }

        protected override void OnClean()
        {
            base.OnClean();
            if (_visualArgs != null)
                _visualArgs.Target.color = _originalColor;
        }

        private void Reset()
        {
            _inputField = GetComponent<TMP_InputField>();
        }

        protected override void UpdateInput(TValue value)
        {
            _lastValidValue = value;
            ApplyDisplayValue(FormatValue(value));
        }

        protected virtual void ApplyDisplayValue(string text)
        {
            _inputField.SetTextWithoutNotify(text);
        }

        protected override void AddInputListener()
        {
            _inputField.onValueChanged.AddListener(OnInputValueChanged);
            _inputField.onEndEdit.AddListener(OnInputEndEdit);
        }

        protected override void RemoveInputListener()
        {
            _inputField.onValueChanged.RemoveListener(OnInputValueChanged);
            _inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        private void OnInputValueChanged(string input)
        {
            ApplyVisualFeedback(input);
            if (_updateMode == TextInputUpdateMode.OnValueChanged)
                ParseAndSetValue(input);
        }

        private void OnInputEndEdit(string input)
        {
            if (_inputField.wasCanceled) return;

            if (_isEmptyProhibited && string.IsNullOrWhiteSpace(input))
            {
                string revertedText = FormatValue(_lastValidValue) ?? string.Empty;
                ApplyDisplayValue(revertedText);
                ApplyVisualFeedback(revertedText);
                return;
            }

            if (_updateMode == TextInputUpdateMode.OnSubmit)
                ParseAndSetValue(input);
        }

        private void ApplyVisualFeedback(string input)
        {
            if (_visualArgs == null || !_isEmptyProhibited) return;
            _visualArgs.Target.color = string.IsNullOrWhiteSpace(input)
                ? _visualArgs.InvalidColor
                : _originalColor;
        }

        protected abstract bool TryParseInput(string input, out TValue result);

        private void ParseAndSetValue(string input)
        {
            if (_isEmptyProhibited && string.IsNullOrWhiteSpace(input)) return;
            if (TryParseInput(input, out TValue result))
                SetValue(result);
            else
                OnParseFailure(input);
        }

        protected virtual void OnParseFailure(string input)
        {
            Debug.LogWarning($"{GetType().Name}: Could not parse input \"{input}\" as {typeof(TValue).Name}.", this);
            ParsingFailed?.Invoke(input);
        }

        private string FormatValue(TValue value)
        {
            return _formatter(value);
        }
    }
}
