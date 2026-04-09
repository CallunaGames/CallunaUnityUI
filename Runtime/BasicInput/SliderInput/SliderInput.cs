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
            _slider.SetValueWithoutNotify(ToSliderValue(value));
        }

        protected override void AddInputListener()
        {
            _slider.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _slider.onValueChanged.RemoveListener(SetValue);
        }

        protected abstract float ToSliderValue(TValue value);
        protected abstract TValue FromSliderValue(float sliderValue);

        private void SetValue(float sliderValue)
        {
            SetValue(FromSliderValue(sliderValue));
        }
    }
}