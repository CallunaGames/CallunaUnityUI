using System;
using System.Collections;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class RollingNumber<T> : MonoBehaviour, Injectable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _duration = 0.33f;

        private CoroutineHelper _coroutineHelper;
        private ReadonlyObservable<T> _value;
        private Func<float, float> _easeFunction = f => f;
        private Func<T, string> _formatValueFunction = f => f.ToString();
        private T _currentValue;
        private string _id;
        private bool _rollOnInit;
        private float _currentDuration;

        void Injectable.Inject(Resolver resolver)
        {
            _coroutineHelper = resolver.Resolve<CoroutineHelper>();
            _id = GetInstanceID().ToString();
            _currentDuration = _duration;
        }

        public RollingNumber<T> WithValue(ReadonlyObservable<T> value)
        {
            if (_value != null)
                _value.OnChanged -= OnValueChanged;

            _value = value;
            _value.OnChanged += OnValueChanged;
            _currentValue = _value.Value;
            return this;
        }

        public RollingNumber<T> WithFormat(Func<T, string> formatFunction)
        {
            _formatValueFunction = formatFunction;
            return this;
        }

        public RollingNumber<T> WithEase(Func<float, float> easeFunction)
        {
            _easeFunction = easeFunction;
            return this;
        }

        public RollingNumber<T> WithRollOnInit(bool roll = true)
        {
            _rollOnInit = roll;
            return this;
        }

        public RollingNumber<T> WithDuration(float duration)
        {
            _currentDuration = duration;
            return this;
        }

        public void Init()
        {
            if (_value == null)
                throw new InvalidOperationException("Failed to init. Please set a value.");
            _currentValue = _value.Value;
            _coroutineHelper.StartWithID(Roll(_rollOnInit ? _currentDuration : 0f), _id);
        }

        void Cleanable.Clean()
        {
            if (_value != null)
                _value.OnChanged -= OnValueChanged;
        }

        protected void Reset()
        {
            _text = transform.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnValueChanged()
        {
            _coroutineHelper.ReplaceWithID(Roll(_currentDuration), _id);
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
            
            UpdateText(_value.Value);
        }

        private void UpdateText(T value)
        {
            _text.text = _formatValueFunction(value);
        }

        protected abstract T GetCurrentValue(T startValue, T targetValue, float t);
    }
}