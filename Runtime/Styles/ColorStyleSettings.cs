using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Calluna.UI
{
    [CreateAssetMenu(fileName = "ColorStyleSettings", menuName = "Calluna Games/Color Style Settings")]
    public class ColorStyleSettings : ScriptableObject
    {
        [SerializeField] private List<ColorStyleSetting> _settings;
        private static Dictionary<string, ColorStyleSetting> _colorStyleSettings;

        public Color GetColorOf(ColorStyle style)
        {
            ColorStyleSetting setting = _settings.FirstOrDefault(s => s.Style.EnumValue == style.EnumValue);
            if(setting != null)
                return setting.Color;
            throw new ArgumentException($"There is no color defined for the style '{style.EnumValue}'.");
        }

        [Serializable]
        public class ColorStyleSetting
        {
            [field: SerializeField] public Color Color { get; private set; }
            [field: SerializeField] public ColorStyle Style { get; private set; }
        }
    }
}
