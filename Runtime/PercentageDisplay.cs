using System;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.Process.View
{
    public class PercentageDisplay : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField, Range(0, 2)] private int _decimalDigits = 0;

        private ReadonlyObservable<float> _value;

        public void Inject(Resolver resolver)
        {
            _value = resolver.Resolve<ReadonlyObservable<float>>();
        }

        private void Reset()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Initialize()
        {
            _value.OnChanged += UpdateText;
            UpdateText();
        }

        public void Clean()
        {
            _value.OnChanged -= UpdateText;
        }

        private void UpdateText()
        {
            string format = $"F{_decimalDigits}";
            _text.text = $"{(_value.Value * 100).ToString(format)}%";
        }
    }
}
