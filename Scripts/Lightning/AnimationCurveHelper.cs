using UnityEngine;

namespace Lightning
{
    public static class AnimationCurveHelper
    {
        /// <summary>
        /// Damped sine wave: starts at 1, oscillates, decays toward 0.
        /// </summary>
        public static float DampedSine(float time, float damping, float frequency)
        {
            if (time <= 0f) return 1f;
            return Mathf.Exp(-damping * time) * Mathf.Cos(frequency * time);
        }

        /// <summary>
        /// Normalizes elapsed to [0,1] over duration, clamped.
        /// </summary>
        public static float ProgressionT(float elapsed, float duration)
        {
            return Mathf.Clamp01(elapsed / duration);
        }
    }
}
