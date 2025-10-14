using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public abstract class SliderInput<TValue> : BasicInput<TValue>
    {
        [SerializeField] private Slider _slider;

        private void Reset()
        {
            _slider = GetComponent<Slider>();
        }

        protected override void UpdateInput(TValue value)
        {
            _slider.SetValueWithoutNotify(ParseValue(value));
        }

        protected override void AddInputListener()
        {
            _slider.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _slider.onValueChanged.RemoveListener(SetValue);
        }
        
        protected abstract float ParseValue(TValue value);
        protected abstract TValue ParseInput(float input);

        private void SetValue(float inputValue)
        {
            SetValue(ParseInput(inputValue));
        }
    }
}