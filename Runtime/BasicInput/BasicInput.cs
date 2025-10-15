using System;
using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class BasicInput<TValue> : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        private Observable<TValue> _observableValue;
        
        public virtual void Inject(Resolver resolver)
        {
            _observableValue = resolver.Resolve<Observable<TValue>>();
        }

        public virtual void Initialize()
        {
            _observableValue.OnChanged += UpdateInput;
            UpdateInput(_observableValue.Value);
            AddInputListener();
        }

        public virtual void Clean()
        {
            _observableValue.OnChanged -= UpdateInput;
            RemoveInputListener();
        }

        protected void SetValue(TValue value)
        {
            _observableValue.Value = value;
        }
        
        protected abstract void UpdateInput(TValue value);
        protected abstract void AddInputListener();
        protected abstract void RemoveInputListener();

        private void UpdateInput()
        {
            UpdateInput(_observableValue.Value);
        }
    }
}
