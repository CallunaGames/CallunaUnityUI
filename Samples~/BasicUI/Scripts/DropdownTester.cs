using System.Collections;
using System.Collections.Generic;
using Calluna.DI;
using TMPro;
using UnityEngine;

namespace Calluna.UI.Samples
{
    public class DropdownTester : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private float _optionsChangeDelay = 0.5f;
        [SerializeField] private List<TMP_Dropdown.OptionData> _maxOptions = new List<TMP_Dropdown.OptionData>();
        private ObservableList<TMP_Dropdown.OptionData> _options;
        private int _index = 0;
        
        void Injectable.Inject(Resolver resolver)
        {
            _options = resolver.Resolve<ObservableList<TMP_Dropdown.OptionData>>();
        }

        void Initializable.Initialize()
        {
            StartCoroutine(ChangeOptions());
        }

        void Cleanable.Clean()
        {
            StopAllCoroutines();
        }

        private IEnumerator ChangeOptions()
        {
             yield return new WaitForSeconds(_optionsChangeDelay);
             if (_index >= _maxOptions.Count)
             {
                 _index = 0;
                 _options.Clear();
             }
             else
             {
                 _options.Add(_maxOptions[_index]);
                 _index++;
             }
             StartCoroutine(ChangeOptions());
        }
    }
}
