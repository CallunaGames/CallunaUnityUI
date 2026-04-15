using UnityEngine;

namespace Calluna.UI
{
    public class FloatRollingNumber : RollingNumber<float>
    {
        protected override float GetCurrentValue(float startValue, float targetValue, float t)
            => Mathf.Lerp(startValue, targetValue, t);
    }
}
