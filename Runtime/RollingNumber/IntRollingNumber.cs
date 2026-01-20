namespace Calluna.UI
{
    public class IntRollingNumber : RollingNumber<int>
    {
        protected override int GetCurrentValue(int startValue, int targetValue, float t)
        {
            return (int)((targetValue - startValue) * t + startValue);
        }
    }
}
