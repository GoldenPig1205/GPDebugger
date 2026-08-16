using System;
using UnityEngine;

namespace GPDebugger.Features
{
    internal static class DebugTimeManager
    {
        internal const float DefaultScale = 1f;
        internal const float MaximumScale = 10f;

        internal static float CurrentScale => Time.timeScale;
        internal static bool IsPaused => Mathf.Approximately(Time.timeScale, 0f);
        internal static bool IsModified { get; private set; }

        internal static bool TrySetScale(float scale, out string error)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale))
            {
                error = "Time scale must be a finite number.";
                return false;
            }

            if (scale < 0f || scale > MaximumScale)
            {
                error = $"Time scale must be between 0 and {MaximumScale:0.##}.";
                return false;
            }

            Time.timeScale = scale;
            IsModified = !Mathf.Approximately(scale, DefaultScale);
            error = null;
            return true;
        }

        internal static void Restore()
        {
            if (!IsModified && Mathf.Approximately(Time.timeScale, DefaultScale))
                return;

            Time.timeScale = DefaultScale;
            IsModified = false;
        }
    }
}
