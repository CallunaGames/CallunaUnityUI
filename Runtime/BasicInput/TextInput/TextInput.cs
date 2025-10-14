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

        public override void Inject(Resolver resolver)
        {
            base.Inject(resolver);
            _cultureInfo = resolver.ResolveOptional<CultureInfo>() ?? CultureInfo.InvariantCulture;
        }

        private void Reset()
        {
            _inputField = GetComponent<TMP_InputField>();
        }
        
        protected override void UpdateInput(TValue value)
        {
            _inputField.text = ParseValue(value);
        }

        protected override void AddInputListener()
        {
            _inputField.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _inputField.onValueChanged.RemoveListener(SetValue);
        }
        
        protected abstract TValue ParseInput(string input);

        private void SetValue(string inputValue)
        {
            SetValue(ParseInput(inputValue));
        }

        private string ParseValue(TValue value)
        {
            if(value is IFormattable formattable)
                _inputField.SetTextWithoutNotify(formattable.ToString(_format, _cultureInfo));
            return value?.ToString();
        }
    }
}