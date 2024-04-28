using System.Collections.Generic;
using UnityEngine;

public class DebugTools
{
    public static void Log(object message, Object context = default)
    {
#if DEBUG
        Debug.Log(message, context);
#endif
    }

    public static void LogWarning(object message, Object context = default)
    {
#if DEBUG
        Debug.LogWarning(message, context);
#endif
    }

    public static void LogError(object message, Object context = default)
    {
#if DEBUG
        Debug.LogError(message, context);
#endif
    }

    public static void LogList<T>(string text, ICollection<T> list, Object context = default)
    {
#if DEBUG
        LogList(text, list, false, context);
#endif
    }

    public static void LogErrorList<T>(string text, ICollection<T> list, Object context = default)
    {
#if DEBUG
        LogList(text, list, true, context);
#endif
    }

    private static void LogList<T>(string text, ICollection<T> list, bool isError, Object context = default)
    {
        var enumerator = list.GetEnumerator();
        var logData = $"{text}: ";
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            logData += $"<{current}>";
        }
        if (isError)
        {
            Debug.LogError(logData, context);
        }
        else
        {
            Debug.Log(logData, context);
        }
    }
}
