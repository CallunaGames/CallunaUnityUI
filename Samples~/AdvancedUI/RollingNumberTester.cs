using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI.Samples.AdvancedUI
{
    public class RollingNumberTester : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private Button _button;
        [SerializeField] private Vector2 valueRange = new Vector2(10, 100);
        [SerializeField] private FloatRollingNumber _floatRollingNumber;
        [SerializeField] private IntRollingNumber _intRollingNumber;

        private Observable<int> _observableInt;
        private Observable<float> _observableFloat;

        public void Inject(Resolver resolver)
        {
            _observableInt = resolver.Resolve<Observable<int>>();
            _observableFloat = resolver.Resolve<Observable<float>>();
        }

        public void Initialize()
        {
            _button.onClick.AddListener(OnClick);
            _floatRollingNumber.WithEase(Tween.EaseInCubic)
                .WithValue(_observableFloat)
                .WithFormat(v => v.ToString("F2"))
                .Init();
            _intRollingNumber.WithEase(Tween.EaseInBack)
                .WithValue(_observableInt)
                .WithFormat(v => v.ToString("F0"))
                .Init();
        }

        public void Clean()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            float value = Random.Range(valueRange.x, valueRange.y);
            _observableInt.Value += (int)value;
            _observableFloat.Value += value;
        }
    }
}