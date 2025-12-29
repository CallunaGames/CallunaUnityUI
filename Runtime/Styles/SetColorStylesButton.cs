using System;
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
        
        public void Inject(Resolver resolver)
        {
            _observableSettings = resolver.Resolve<Observable<ColorStyleSettings>>();
        }

        public void Initialize()
        {
            _button.onClick.AddListener(SetStyle);
        }

        public void Clean()
        {
            _button.onClick.RemoveListener(SetStyle);
        }

        private void Reset()
        {
            _button = GetComponent<Button>();
        }

        private void SetStyle()
        {
            if(_observableSettings.Value != _colorStyleSettings)
                _observableSettings.Value = _colorStyleSettings;
        }
    }
}
