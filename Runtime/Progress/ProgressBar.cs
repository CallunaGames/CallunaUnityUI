using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ProgressBar : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Slider _slider;

        private ReadonlyObservable<float> _progress;

        public void Inject(Resolver resolver)
        {
            _progress = resolver.Resolve<ReadonlyObservable<float>>();
        }

        private void Reset()
        {
            _slider = GetComponentInChildren<Slider>();
        }

        public void Initialize()
        {
            _progress.OnChanged += UpdateSlider;
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            UpdateSlider();
        }

        public void Clean()
        {
            _progress.OnChanged -= UpdateSlider;
        }

        private void UpdateSlider()
        {
            _slider.value = Mathf.Clamp01(_progress.Value);
        }
    }
}
