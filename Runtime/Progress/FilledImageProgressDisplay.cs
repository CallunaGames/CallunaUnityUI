using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class FilledImageProgressDisplay : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Image _image;

        private ReadonlyObservable<float> _progress;

        public void Inject(Resolver resolver)
        {
            _progress = resolver.Resolve<ReadonlyObservable<float>>();
        }

        private void Reset()
        {
            _image = GetComponentInChildren<Image>();
        }

        public void Initialize()
        {
            _progress.OnChanged += UpdateFillAmount;
            UpdateFillAmount();
        }

        public void Clean()
        {
            _progress.OnChanged -= UpdateFillAmount;
        }

        private void UpdateFillAmount()
        {
            _image.fillAmount = Mathf.Clamp01(_progress.Value);
        }
    }
}
