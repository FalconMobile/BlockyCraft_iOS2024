using System.Collections.Generic;
using UnityEngine;

public class Yielders : MonoBehaviour
{
    private class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y) => x == y;

        int IEqualityComparer<float>.GetHashCode(float obj) => obj.GetHashCode();
    }

    private static readonly Dictionary<float, WaitForSeconds> TimeIntervals =
        new Dictionary<float, WaitForSeconds>(50, new FloatComparer());

    private static readonly Dictionary<float, WaitForSecondsRealtime> RealTimeIntervals =
        new Dictionary<float, WaitForSecondsRealtime>(50, new FloatComparer());
    
    public static WaitForEndOfFrame EndOfFrame { get; } = new WaitForEndOfFrame();

    public static WaitForFixedUpdate FixedUpdate { get; } = new WaitForFixedUpdate();

    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        if (!TimeIntervals.TryGetValue(seconds, out var waitForSeconds))
        {
            TimeIntervals.Add(seconds, waitForSeconds = new WaitForSeconds(seconds));
        }

        return waitForSeconds;
    }

    public static object WaitForSecondsRealtime(float seconds)
    {
        if (!RealTimeIntervals.TryGetValue(seconds, out var waitForSeconds))
        {
            RealTimeIntervals.Add(seconds, waitForSeconds = new WaitForSecondsRealtime(seconds));
        }

        return waitForSeconds;
    }
}
