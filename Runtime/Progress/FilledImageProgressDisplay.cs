using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class FilledImageProgressDisplay : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Image _image;

        private ReadonlyObservable<float> _progress;

        void Injectable.Inject(Resolver resolver)
        {
            _progress = resolver.Resolve<ReadonlyObservable<float>>();
        }

        private void Reset()
        {
            _image = GetComponentInChildren<Image>();
        }

        void Initializable.Initialize()
        {
            _progress.OnChanged += UpdateFillAmount;
            UpdateFillAmount();
        }

        void Cleanable.Clean()
        {
            _progress.OnChanged -= UpdateFillAmount;
        }

        private void UpdateFillAmount()
        {
            _image.fillAmount = Mathf.Clamp01(_progress.Value);
        }
    }
}
