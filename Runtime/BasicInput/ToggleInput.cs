using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ToggleInput : BasicInput<bool>
    {
        [SerializeField] private Toggle _toggle;

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        protected override void UpdateInput(bool value)
        {
            _toggle.SetIsOnWithoutNotify(value);
        }

        protected override void AddInputListener()
        {
            _toggle.onValueChanged.AddListener(SetValue);
        }

        protected override void RemoveInputListener()
        {
            _toggle.onValueChanged.RemoveListener(SetValue);
        }
    }
}
