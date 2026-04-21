namespace Calluna.UI
{
    public class IntRollingNumber : RollingNumber<int>
    {
        protected override int Interpolate(int startValue, int targetValue, float t)
            // Mathf.Lerp only supports float; replicate it for int and truncate toward startValue.
            => (int)((targetValue - startValue) * t + startValue);
    }
}
