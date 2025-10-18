using System;
using UnityEngine;

namespace Calluna.UI
{
    [Serializable]
    public class ColorStyle : IEquatable<ColorStyle>
    {
        [field: SerializeField] public int EnumValue { get; private set; }
        [field: SerializeField] public int SelectedEnumIndex {get; set; }
        [field: SerializeField] public string SelectedEnumName {get; set; }

        public bool Equals(ColorStyle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return EnumValue == other.EnumValue && SelectedEnumIndex == other.SelectedEnumIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ColorStyle)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EnumValue, SelectedEnumIndex);
        }
    }
}
