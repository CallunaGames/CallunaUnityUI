using System.Collections.Generic;
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
        private bool _areOptionsDirty;
        private TMP_Dropdown.OptionData _selectedOption;
        private List<TMP_Dropdown.OptionData> _optionsBuffer;

        private void Reset()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
        }

        protected override void OnInject(Resolver resolver)
        {
            base.OnInject(resolver);
            _options = resolver.Resolve<ReadonlyObservableList<TMP_Dropdown.OptionData>>();
            _optionChangeDetector = new ObservableListChangeDetector<TMP_Dropdown.OptionData>(_options);
            _optionsBuffer = new List<TMP_Dropdown.OptionData>();
        }

        private void Update()
        {
            if (_areOptionsDirty)
                FlushDirtyOptions();
        }

        internal void FlushDirtyOptions()
        {
            _areOptionsDirty = false;
            UpdateOptions();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _optionChangeDetector.OnChanged += SetOptionsDirty;
            UpdateOptions();
        }

        protected override void OnClean()
        {
            base.OnClean();
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
            _optionsBuffer.Clear();
            _optionsBuffer.AddRange(_options);
            _dropdown.ClearOptions();
            _dropdown.AddOptions(_optionsBuffer);
            _dropdown.SetValueWithoutNotify(_observableValue.Value);
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
            int index = GetSelectedIndex();
            if(index != _observableValue.Value)
                SetValue(index);
        }

        // After a list rebuild, recover the previously selected option by identity.
        // If it no longer exists in the new list, fall back to index 0 (or -1 if the list is empty).
        private int GetSelectedIndex()
        {
            if(_selectedOption == null)
                return _options.Count > 0 ? 0 : -1;
            int index = _options.IndexOf(_selectedOption);
            return index < 0 && _options.Count > 0 ? 0 : index;
        }
    }
}