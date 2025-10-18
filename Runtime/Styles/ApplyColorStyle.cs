using System;
using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ApplyColorStyle : MonoBehaviour, Injectable, Initializable
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField, Space] private ColorStyle _colorStyle;

        private ColorStyleSettings _styleSettings;
        
        public void Inject(Resolver resolver)
        {
            _styleSettings = resolver.Resolve<ColorStyleSettings>();
        }

        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
        }

        public void Initialize()
        {
            _graphic.color = _styleSettings.GetColorOf(_colorStyle);
        }
    }
}
