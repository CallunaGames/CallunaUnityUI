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

        private CoroutineHelper _coroutineHelper;
        private ReadonlyObservable<T> _value;
        private Func<float, float> _easeFunction = f => f;
        private Func<T, string> _formatter = f => f is IFormattable fmt ? fmt.ToString(null, null) : f?.ToString() ?? string.Empty;
        private T _currentValue;
        private string _coroutineId;
        private bool _shouldRollOnInit;
        private float _activeDuration;
        private RollingNumberAnimator<T> _animator;
        private bool _isApplied;

        void Injectable.Inject(Resolver resolver)
        {
            _coroutineHelper = resolver.Resolve<CoroutineHelper>();
            _coroutineId = GetHashCode().ToString();
            _activeDuration = _duration;
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
            _formatter = formatFunction;
            return this;
        }

        public RollingNumber<T> WithEase(Func<float, float> easeFunction)
        {
            _easeFunction = easeFunction;
            return this;
        }

        public RollingNumber<T> WithRollOnInit(bool roll = true)
        {
            _shouldRollOnInit = roll;
            return this;
        }

        public RollingNumber<T> WithDuration(float duration)
        {
            _activeDuration = duration;
            return this;
        }

        /// <summary>
        /// Called by the DI container. Auto-applies if <see cref="Apply"/> has not yet been
        /// called manually, so components that configure the rolling number in their own
        /// <c>Initialize()</c> don't need to guard against double-application.
        /// </summary>
        void Initializable.Initialize()
        {
            if (_value != null && !_isApplied)
                Apply();
        }

        /// <summary>
        /// Applies the current configuration and starts (or restarts) the display.
        /// Call this at the end of the fluent <c>With*</c> chain, or again at runtime
        /// to pick up changed options such as a new ease function or duration.
        /// </summary>
        public void Apply()
        {
            if (_value == null)
                throw new InvalidOperationException("Cannot apply: no value has been set. Call WithValue() first.");
            _isApplied = true;
            _animator = new RollingNumberAnimator<T>(GetCurrentValue, _easeFunction, _formatter);
            _currentValue = _value.Value;
            _coroutineHelper.ReplaceWithID(Roll(_shouldRollOnInit ? _activeDuration : 0f), _coroutineId);
        }

        void Cleanable.Clean()
        {
            _isApplied = false;
            if (_value != null)
                _value.OnChanged -= OnValueChanged;
        }

        protected void Reset()
        {
            _text = transform.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnValueChanged()
        {
            _coroutineHelper.ReplaceWithID(Roll(_activeDuration), _coroutineId);
        }

        private IEnumerator Roll(float duration)
        {
            if (duration <= 0)
            {
                _currentValue = _value.Value;
                UpdateText(_currentValue);
                yield break;
            }

            float elapsed = 0f;
            T startValue = _currentValue;
            while (elapsed < duration)
            {
                _currentValue = _animator.Step(startValue, _value.Value, elapsed, duration);
                UpdateText(_currentValue);
                elapsed += GetDeltaTime();
                yield return null;
            }

            UpdateText(_value.Value);
        }

        protected virtual float GetDeltaTime() => Time.deltaTime;

        protected virtual void UpdateText(T value)
        {
            _text.text = _animator.Format(value);
        }

        protected abstract T GetCurrentValue(T startValue, T targetValue, float t);
    }
}
