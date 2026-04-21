using UnityEngine;

namespace Calluna.UI
{
    public class FloatRollingNumber : RollingNumber<float>
    {
        protected override float Interpolate(float startValue, float targetValue, float t)
            => Mathf.Lerp(startValue, targetValue, t);
    }
}
