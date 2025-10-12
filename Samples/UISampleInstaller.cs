using Calluna.DI;

namespace Calluna.UI.Samples
{
    public class UISampleInstaller : MonoInstaller
    {
        private Observable<float> _value = new Observable<float>();
        private Observable<string> _label = new Observable<string>();
        
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<ReadonlyObservable<float>>().And<Observable<float>>().ToInstance(_value);
            binder.Bind<ReadonlyObservable<string>>().And<Observable<string>>().ToInstance(_label);
        }
    }
}
