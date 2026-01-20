using System;
using System.Collections;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class RollingNumber<T> : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _duration = 0.33f;
        [SerializeField] private bool _rollOnStart = false;

        private ReadonlyObservable<T> _value;
        private Func<float, float> _easeFunction;
        private Func<T, string> _formatValueAction;
        private T _currentValue;
        private Coroutine _routine;
        
        public void Inject(Resolver resolver)
        {
            Arguments arguments = resolver.Resolve<Arguments>();
            _value = arguments.Value;
            _easeFunction = arguments.EaseFunction;
            _formatValueAction = arguments.FormatValueAction;
            _currentValue = _value.Value;
        }
        
        public void Initialize()
        {
            _value.OnChanged += OnValueChanged;
            _currentValue = _value.Value;
            _routine = StartCoroutine(Roll(_rollOnStart ? _duration : 0f));
        }

        public void Clean()
        {
            _value.OnChanged -= OnValueChanged;
        }

        protected void Reset()
        {
            _text = transform.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnValueChanged()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _routine = StartCoroutine(Roll(_duration));
        }

        private IEnumerator Roll(float duration)
        {
            if (duration <= 0)
            {
                _currentValue = _value.Value;
                UpdateText(_currentValue);
                yield break;
            }

            float elapsedTime = 0f;
            T startValue = _currentValue;
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                t = _easeFunction(t);
                _currentValue = GetCurrentValue(startValue, _value.Value, t);
                UpdateText(_currentValue);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private void UpdateText(T value)
        {
            _text.text = _formatValueAction(value);
        }

        protected abstract T GetCurrentValue(T startValue, T targetValue, float t);

        public class Arguments
        {
            public Func<T, string> FormatValueAction = v => v.ToString();
            public Func<float, float> EaseFunction = f => f;
            public ReadonlyObservable<T> Value;
        }
    }
}
