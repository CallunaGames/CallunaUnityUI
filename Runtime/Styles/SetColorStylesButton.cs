using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class SetColorStylesButton : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private ColorStyleSettings _colorStyleSettings;
        [SerializeField] private Button _button;

        private Observable<ColorStyleSettings> _observableSettings;
        
        void Injectable.Inject(Resolver resolver)
        {
            _observableSettings = resolver.Resolve<Observable<ColorStyleSettings>>();
        }

        void Initializable.Initialize()
        {
            _button.onClick.AddListener(SetStyle);
        }

        void Cleanable.Clean()
        {
            _button.onClick.RemoveListener(SetStyle);
        }

        private void Reset()
        {
            _button = GetComponent<Button>();
        }

        private void SetStyle()
        {
            // ScriptableObject identity is used intentionally: Unity always returns the same
            // asset reference, so reference equality is equivalent to "same theme".
            if (_observableSettings.Value != _colorStyleSettings)
                _observableSettings.Value = _colorStyleSettings;
        }
    }
}
