using System;
using System.Collections;
using UnityEngine;

namespace TaskSystem
{
    public class CoroutineContainer : MonoBehaviour
    {
        private static CoroutineContainer _instance;

        private static CoroutineContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    Init();
                }
                return _instance;
            }
        }

        private static void Init()
        {
            GameObject go = new GameObject();
            _instance = go.AddComponent<CoroutineContainer>();
            go.name = nameof(CoroutineContainer);
            DontDestroyOnLoad(go);
        }

        public static Coroutine Start(IEnumerator routine) => 
            Instance.StartCoroutine(routine);

        public static void Stop(Coroutine coroutine) => 
            Instance.StopCoroutine(coroutine);

        public static Coroutine DelayAction(float seconds, Action action) => 
            Instance.StartCoroutine(DelayActionRoutine(seconds, action));

        public static Coroutine DelayAction(float seconds, Action action, MonoBehaviour onMonoBeh)
        {
            if (onMonoBeh == null || !onMonoBeh.isActiveAndEnabled)
            {
                Debug.Log("Trying to start coroutine on destroyed or inactive MonoBehaviour. Return null without consequences");
                return null;
            }
            return onMonoBeh.StartCoroutine(DelayActionRoutine(seconds, action));
        }

        private static IEnumerator DelayActionRoutine(float seconds, Action action)
        {
            yield return Yielders.WaitForSecondsRealtime(seconds);
            action.Invoke();
        }
    }
}