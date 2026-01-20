using Calluna.DI;
using UnityEngine;

namespace Calluna.UI.Samples.AdvancedUI
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<ReadonlyObservable<int>>().And<Observable<int>>().ToNew<Observable<int>>().AsSingle();
            binder.Bind<ReadonlyObservable<float>>().And<Observable<float>>().ToNew<Observable<float>>().AsSingle();
        }
    }
}
