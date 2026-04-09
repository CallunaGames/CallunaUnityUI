using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ClearColorStylesButton : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Button _button;

        private Observable<ColorStyleSettings> _observableSettings;
        
        void Injectable.Inject(Resolver resolver)
        {
            _observableSettings = resolver.Resolve<Observable<ColorStyleSettings>>();
        }

        void Initializable.Initialize()
        {
            _button.onClick.AddListener(ClearStyle);
        }

        void Cleanable.Clean()
        {
            _button.onClick.RemoveListener(ClearStyle);
        }

        private void Reset()
        {
            _button = GetComponent<Button>();
        }

        private void ClearStyle()
        {
            if(_observableSettings.HasValue)
                _observableSettings.Value = null;
        }
    }
}
