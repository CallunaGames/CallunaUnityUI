using System.Collections;
using System.Collections.Generic;
using Calluna.DI;
using UnityEngine;

namespace Calluna.UI.Samples
{
    public class UITester : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private List<string> _labels = new List<string>();
        [SerializeField] private float _changeFrequency = 0.33f;
        
        private Observable<float> _progress;
        private Observable<string> _label;
        private Observable<int> _intValue;
        private float _delta;
        
        public void Inject(Resolver resolver)
        {
            _progress = resolver.Resolve<Observable<float>>();
            _intValue = resolver.Resolve<Observable<int>>();
            _label = resolver.Resolve<Observable<string>>();
        }

        public void Initialize()
        {
            _delta = _labels.Count > 0 ? 1f / _labels.Count : 1;
            StartCoroutine(RotateLabels());
        }

        public void Clean()
        {
            StopAllCoroutines();
        }

        private IEnumerator RotateLabels()
        {
            _label.Value = _labels[_intValue.Value];
            _progress.Value = _progress.Value < 1 ? _progress.Value + _delta : (_progress.Value + _delta) % 1f;
            _intValue.Value = (_intValue.Value + 1) % _labels.Count;
            yield return new WaitForSeconds(_changeFrequency);
            StartCoroutine(RotateLabels());
        }
    }
}
