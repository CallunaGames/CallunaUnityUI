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
        [SerializeField] private bool _prohibitEmpty;
        [SerializeField] private TextInputUpdateMode _updateMode = TextInputUpdateMode.OnValueChanged;

        public event Action<string> ParsingFailed;

        private CultureInfo _cultureInfo;
        private TextInputVisualArgs _visualArgs;
        private Color _originalColor;
        private TValue _lastValidValue;

        protected override void OnInject(Resolver resolver)
        {
            base.OnInject(resolver);
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
            _visualArgs = resolver.ResolveOptional<TextInputVisualArgs>();
        }

        protected override void OnInitialize()
        {
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
            _inputField.onValueChanged.AddListener(OnValueChangedHandler);
            _inputField.onEndEdit.AddListener(OnEndEditHandler);
        }

        protected override void RemoveInputListener()
        {
            _inputField.onValueChanged.RemoveListener(OnValueChangedHandler);
            _inputField.onEndEdit.RemoveListener(OnEndEditHandler);
        }

        private void OnValueChangedHandler(string input)
        {
            ApplyVisualFeedback(input);
            if (_updateMode == TextInputUpdateMode.OnValueChanged)
                ParseAndSetValue(input);
        }

        private void OnEndEditHandler(string input)
        {
            if (_inputField.wasCanceled) return;

            if (_prohibitEmpty && string.IsNullOrWhiteSpace(input))
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
            if (_visualArgs == null || !_prohibitEmpty) return;
            _visualArgs.Target.color = string.IsNullOrWhiteSpace(input)
                ? _visualArgs.InvalidColor
                : _originalColor;
        }

        protected abstract bool TryParseInput(string input, out TValue result);

        private void ParseAndSetValue(string input)
        {
            if (_prohibitEmpty && string.IsNullOrWhiteSpace(input)) return;
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
            if (value is IFormattable formattable)
                return formattable.ToString(_format, _cultureInfo);
            return value?.ToString();
        }
    }
}
