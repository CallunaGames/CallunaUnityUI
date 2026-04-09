using System;
using Calluna.DI;
using UnityEngine;

namespace Calluna.UI
{
    public abstract class BasicInput<TValue> : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        protected Observable<TValue> _observableValue;
        
        void Injectable.Inject(Resolver resolver) => OnInject(resolver);
        void Initializable.Initialize() => OnInitialize();
        void Cleanable.Clean() => OnClean();

        protected virtual void OnInject(Resolver resolver)
        {
            _observableValue = resolver.Resolve<Observable<TValue>>();
        }

        protected virtual void OnInitialize()
        {
            _observableValue.OnChanged += UpdateInput;
            UpdateInput(_observableValue.Value);
            AddInputListener();
        }

        protected virtual void OnClean()
        {
            _observableValue.OnChanged -= UpdateInput;
            RemoveInputListener();
        }

        protected virtual void SetValue(TValue value)
        {
            _observableValue.Value = value;
        }
        
        protected abstract void UpdateInput(TValue value);
        protected abstract void AddInputListener();
        protected abstract void RemoveInputListener();

        // Bridge overload: matches the parameterless OnChanged delegate signature.
        private void UpdateInput()
        {
            UpdateInput(_observableValue.Value);
        }
    }
}
