using Calluna.DI;

namespace Calluna.UI.Samples
{
    public class ColorStyleInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            Observable<ColorStyleSettings> observable = new Observable<ColorStyleSettings>();
            binder.Bind<ReadonlyObservable<ColorStyleSettings>>()
                .And<Observable<ColorStyleSettings>>()
                .ToInstance(observable);
        }
    }
}
