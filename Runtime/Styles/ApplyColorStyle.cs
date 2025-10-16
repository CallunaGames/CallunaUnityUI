using System;
using Calluna.DI;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.UI
{
    public class ApplyColorStyle : MonoBehaviour, Injectable, Initializable
    {
        [SerializeField] private ColorStyle _colorStyle;
        [SerializeField] private Graphic _graphic;

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
