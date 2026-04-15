using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.UI
{
    [CreateAssetMenu(fileName = "ColorStyleSettings", menuName = "Calluna Games/UI/Color Style/Color Style Settings")]
    public class ColorStyleSettings : ScriptableObject
    {
        [SerializeField] private List<ColorStyleSetting> _settings;

        public Color GetColorOf(ColorStyleId style)
        {
            if (TryGetColorOf(style, out Color color))
                return color;
            throw new ArgumentException($"There is no color defined for the style '{style}'." +
                                        $"Please open the style settings and see if everything is setup correctly.");
        }

        public bool TryGetColorOf(ColorStyleId style, out Color color)
        {
            for (int i = 0; i < _settings.Count; i++)
            {
                ColorStyleSetting setting = _settings[i];
                if (setting.Style == style)
                {
                    color = setting.Color;
                    return true;
                }
            }
            color = default;
            return false;
        }

        [Serializable]
        public class ColorStyleSetting
        {
            [field: SerializeField] public ColorStyleId Style { get; private set; }
            [field: SerializeField] public Color Color { get; private set; }
        }
    }
}
