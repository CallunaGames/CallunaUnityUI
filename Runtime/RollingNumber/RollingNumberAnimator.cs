using System;

namespace Calluna.UI
{
    internal class RollingNumberAnimator<T>
    {
        private readonly Func<T, T, float, T> _interpolate;
        private readonly Func<float, float> _ease;
        private readonly Func<T, string> _format;

        internal RollingNumberAnimator(
            Func<T, T, float, T> interpolate,
            Func<float, float> ease,
            Func<T, string> format)
        {
            _interpolate = interpolate;
            _ease = ease;
            _format = format;
        }

        // Returns the interpolated value at the given elapsed time within duration.
        internal T Step(T startValue, T targetValue, float elapsed, float duration)
        {
            float easedT = _ease(elapsed / duration);
            return _interpolate(startValue, targetValue, easedT);
        }

        internal string Format(T value) => _format(value);
    }
}
