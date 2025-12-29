using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Calluna.UI
{
    [CreateAssetMenu(fileName = "ColorStyleSettings", menuName = "Calluna Games/UI/Color Style/Color Style Settings")]
    public class ColorStyleSettings : ScriptableObject
    {
        [SerializeField] private List<ColorStyleSetting> _settings;

        public Color GetColorOf(ColorStyleId style)
        {
            ColorStyleSetting setting = _settings.FirstOrDefault(s => s.Style.Equals(style));
            if(setting != null)
                return setting.Color;
            throw new ArgumentException($"There is no color defined for the style '{style}'." +
                                        $"Please open the style settings and see if everything is setup correctly.");
        }

        [Serializable]
        public class ColorStyleSetting
        {
            [field: SerializeField] public ColorStyleId Style { get; private set; }
            [field: SerializeField] public Color Color { get; private set; }
        }
    }
}
