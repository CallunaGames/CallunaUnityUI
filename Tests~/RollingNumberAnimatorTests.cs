using NUnit.Framework;

namespace Calluna.UI.Tests
{
    public class RollingNumberAnimatorTests
    {
        private static float LinearInterpolate(float start, float target, float t) => start + (target - start) * t;
        private static float LinearEase(float t) => t;

        private RollingNumberAnimator<float> MakeAnimator(
            System.Func<float, float, float, float> interpolate = null,
            System.Func<float, float> ease = null,
            System.Func<float, string> format = null)
        {
            return new RollingNumberAnimator<float>(
                interpolate ?? LinearInterpolate,
                ease        ?? LinearEase,
                format      ?? (v => v.ToString("F2")));
        }

        [Test]
        public void RollingNumberAnimator_Step_AtElapsedZero_ReturnsStart()
        {
            var animator = MakeAnimator();
            float result = animator.Step(0f, 100f, elapsed: 0f, duration: 1f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void RollingNumberAnimator_Step_AtElapsedEqualsDuration_ReturnsTarget()
        {
            var animator = MakeAnimator();
            float result = animator.Step(0f, 100f, elapsed: 1f, duration: 1f);
            Assert.AreEqual(100f, result, 0.0001f);
        }

        [Test]
        public void RollingNumberAnimator_Step_AtMidpoint_ReturnsHalfway()
        {
            var animator = MakeAnimator();
            float result = animator.Step(0f, 100f, elapsed: 0.5f, duration: 1f);
            Assert.AreEqual(50f, result, 0.0001f);
        }

        [Test]
        public void RollingNumberAnimator_Step_AppliesEaseFunction()
        {
            // Ease function squares t, so at elapsed=0.5 with linear interpolation:
            // easedT = 0.25, result = 25
            var animator = MakeAnimator(ease: t => t * t);
            float result = animator.Step(0f, 100f, elapsed: 0.5f, duration: 1f);
            Assert.AreEqual(25f, result, 0.0001f);
        }

        [Test]
        public void RollingNumberAnimator_Step_WorksWithNegativeDirection()
        {
            var animator = MakeAnimator();
            float result = animator.Step(100f, 0f, elapsed: 0.5f, duration: 1f);
            Assert.AreEqual(50f, result, 0.0001f);
        }

        [Test]
        public void RollingNumberAnimator_Format_UsesSuppliedFormatter()
        {
            var animator = MakeAnimator(format: v => $"${v:F0}");
            string result = animator.Format(42.7f);
            Assert.AreEqual("$43", result);
        }

        [Test]
        public void RollingNumberAnimator_Step_DurationScalesElapsedCorrectly()
        {
            var animator = MakeAnimator();
            // Half of a 2-second duration = t 0.25 of a 1-second duration
            float resultLong  = animator.Step(0f, 100f, elapsed: 1f, duration: 2f);
            float resultShort = animator.Step(0f, 100f, elapsed: 0.5f, duration: 1f);
            Assert.AreEqual(resultShort, resultLong, 0.0001f);
        }
    }
}
