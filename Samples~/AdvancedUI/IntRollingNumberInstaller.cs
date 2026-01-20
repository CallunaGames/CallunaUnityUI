using Calluna.DI;

namespace Calluna.UI.Samples.AdvancedUI
{
    public class IntRollingNumberInstaller : MonoInstaller, Injectable
    {
        private ReadonlyObservable<int> _value;
        
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<RollingNumber<int>.Arguments>().ToInstance(
                new RollingNumber<int>.Arguments
                {
                    Value = _value,
                    EaseFunction = Tween.EaseInOutSine,
                    FormatValueAction = v => v.ToString(),
                });
        }

        public void Inject(Resolver resolver)
        {
            _value = resolver.Resolve<ReadonlyObservable<int>>();
        }
    }
}
