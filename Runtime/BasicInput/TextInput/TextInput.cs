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
        
        private CultureInfo _cultureInfo;

        protected override void OnInject(Resolver resolver)
        {
            base.OnInject(resolver);
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
        }

        private void Reset()
        {
            _inputField = GetComponent<TMP_InputField>();
        }
        
        protected override void UpdateInput(TValue value)
        {
            _inputField.SetTextWithoutNotify(FormatValue(value));
        }

        protected override void AddInputListener()
        {
            _inputField.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _inputField.onValueChanged.RemoveListener(SetValue);
        }
        
        protected abstract bool TryParseInput(string input, out TValue result);

        private void SetValue(string input)
        {
            if (TryParseInput(input, out TValue result))
                SetValue(result);
            else
                OnParseFailure(input);
        }

        protected virtual void OnParseFailure(string input)
        {
            Debug.LogWarning($"{GetType().Name}: Could not parse input \"{input}\" as {typeof(TValue).Name}.", this);
        }

        private string FormatValue(TValue value)
        {
            if (value is IFormattable formattable)
                return formattable.ToString(_format, _cultureInfo);
            return value?.ToString();
        }
    }
}