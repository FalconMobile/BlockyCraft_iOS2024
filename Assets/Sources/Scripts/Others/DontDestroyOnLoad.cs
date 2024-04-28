using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    public List<Initializable> initializableItems = new List<Initializable>();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        foreach (var item in initializableItems)
        {
            item.Init();
        }

#if CHEAT
        SRDebug.Init();
#endif
    }
}