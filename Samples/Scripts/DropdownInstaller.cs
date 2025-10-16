using Calluna.DI;
using TMPro;

namespace Calluna.UI.Samples
{
    public class DropdownInstaller : MonoInstaller
    {
        private ObservableList<TMP_Dropdown.OptionData> _options = new ObservableList<TMP_Dropdown.OptionData>();
        private Observable<int> _indices = new Observable<int>();
        
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<ObservableList<TMP_Dropdown.OptionData>>()
                .And<ReadonlyObservableList<TMP_Dropdown.OptionData>>()
                .ToInstance(_options);
            binder.Bind<Observable<int>>().And<ReadonlyObservable<int>>().ToInstance(_indices);
        }
    }
}
