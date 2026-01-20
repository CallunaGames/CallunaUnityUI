using Calluna.DI;

namespace Calluna.UI.Samples.AdvancedUI
{
    public class FloatRollingNumberInstaller : MonoInstaller, Injectable
    {
        private ReadonlyObservable<float> _value;
        
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<RollingNumber<float>.Arguments>().ToInstance(
                new RollingNumber<float>.Arguments
                {
                    Value = _value,
                    EaseFunction = Tween.EaseInCubic,
                    FormatValueAction = v => v.ToString("F2"),
                });
        }

        public void Inject(Resolver resolver)
        {
            _value = resolver.Resolve<ReadonlyObservable<float>>();
        }
    }
}
