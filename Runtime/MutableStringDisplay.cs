using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.Process.View
{
    public class MutableStringDisplay : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private TextMeshProUGUI _text;

        private ReadonlyObservable<string> _string;

        public void Inject(Resolver resolver)
        {
            _string = resolver.Resolve<ReadonlyObservable<string>>();
        }

        private void Reset()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Initialize()
        {
            _string.OnChanged += UpdateText;
            UpdateText();
        }

        public void Clean()
        {
            _string.OnChanged -= UpdateText;
        }

        private void UpdateText()
        {
            _text.text = _string.Value;
        }
    }
}