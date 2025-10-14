using System;
using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class BasicInput<TValue> : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        private Observable<TValue> _value;
        
        public virtual void Inject(Resolver resolver)
        {
            _value = resolver.Resolve<Observable<TValue>>();
        }

        public void Initialize()
        {
            _value.OnChanged += UpdateInput;
            UpdateInput(_value.Value);
            AddInputListener();
        }

        public void Clean()
        {
            _value.OnChanged -= UpdateInput;
            RemoveInputListener();
        }

        protected void SetValue(TValue value)
        {
            _value.Value = value;
        }
        
        protected abstract void UpdateInput(TValue value);
        protected abstract void AddInputListener();
        protected abstract void RemoveInputListener();

        private void UpdateInput()
        {
            UpdateInput(_value.Value);
        }
    }
}
