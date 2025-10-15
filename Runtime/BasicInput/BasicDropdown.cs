using System.Linq;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI
{
    public class BasicDropdown : BasicInput<int>
    {
        [SerializeField] private TMP_Dropdown _dropdown;

        private ReadonlyObservableList<TMP_Dropdown.OptionData> _options;
        private ObservableListChangeDetector<TMP_Dropdown.OptionData> _optionChangeDetector;
        private bool _areOptionsDirty = false;
        private TMP_Dropdown.OptionData _selectedOption;
        private ReadonlyObservable<int> _index;

        private void Reset()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
        }

        public override void Inject(Resolver resolver)
        {
            base.Inject(resolver);
            _options = resolver.Resolve<ReadonlyObservableList<TMP_Dropdown.OptionData>>();
            _index = resolver.Resolve<ReadonlyObservable<int>>();
            _optionChangeDetector = new ObservableListChangeDetector<TMP_Dropdown.OptionData>(_options);
        }

        private void Update()
        {
            if (_areOptionsDirty)
            {
                _areOptionsDirty = false;
                UpdateOptions();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _optionChangeDetector.OnChanged += SetOptionsDirty;
            UpdateOptions();
        }

        public override void Clean()
        {
            base.Clean();
            _optionChangeDetector.OnChanged -= SetOptionsDirty;
            _optionChangeDetector.Dispose();
        }

        protected override void UpdateInput(int value)
        {
            _selectedOption = value >= 0 && value < _options.Count ? _options[value] : null;
            _dropdown.SetValueWithoutNotify(value);
        }

        protected override void AddInputListener()
        {
            _dropdown.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _dropdown.onValueChanged.RemoveListener(SetValue);
        }

        private void SetOptionsDirty()
        {
            _areOptionsDirty = true;
        }

        private void UpdateOptions()
        {
            _dropdown.ClearOptions();
            _dropdown.AddOptions(_options.ToList());
            _dropdown.SetValueWithoutNotify(_index.Value);
            UpdateShownOptions();
            UpdateSelectedIndex();
        }

        private void UpdateShownOptions()
        {
            if (!_dropdown.IsExpanded)
                return;
            _dropdown.Hide();
            _dropdown.Show();
        }

        private void UpdateSelectedIndex()
        {
            int index = GetIndex();
            if(index != _index.Value)
                SetValue(index);
        }

        private int GetIndex()
        {
            if(_selectedOption == null)
                return _options.Count > 0 ? 0 : -1;
            int index = _options.IndexOf(_selectedOption);
            return index < 0 && _options.Count > 0 ? 0 : index;
        }
    }
}