using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Sources.Scripts.Managers;
using UnityEngine;
using UnityEngine.Networking;

public static class ConfigLoader
{
    public const string GamePrefix = "_BlockyCraft";
    public const string PlatformPrefix =
#if UNITY_ANDROID
        "_Android";
#elif UNITY_IOS
        "_IOS";
#elif UNITY_WEBGL
        "_WEBGL";
#else
        "";
#endif

    public const string ShopLink = 
#if UNITY_ANDROID
        "market://details?id=com.blocky.craft.pixel.building.mini.world";
#elif UNITY_IOS
        "https://apps.apple.com/us/app/solitaire-classic/id380858627";
#elif UNITY_WEBGL
        "_WEBGL";
#else
        "";
#endif
    private const string _configUrl = "YOUR_CONFIG_ADDRESS";
    private static readonly string ConfigURL = $"{_configUrl}Config.xml";

    private static readonly Dictionary<string, string> Data = new Dictionary<string, string>();

    public static event Action OnDataLoaded;
    public static bool isDataReady;

    public static void LoadConfig()
    {
        CoroutineContainer.Start(LoadData(ConfigURL, SaveConfigData));
    }

    private static IEnumerator LoadData(string url, Action<string> saveHandler)
    {
        string error = null;
        for (var i = 0; i < 50; i++)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield return Yielders.WaitForSeconds(3f);
                continue;
            }

            using (var webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError($"Fail get to connect:{url}.<b>Error:</b>{webRequest.error} Wait retry:{i}");
                    error = webRequest.error;

                    if (error.Contains("404"))
                    {
                        yield break;
                    }
                }
                else
                {
                    saveHandler?.Invoke(webRequest.downloadHandler.text);
                    yield break;
                }
            }

            yield return Yielders.WaitForSeconds(3f);
        }

        Debug.LogError($"Fail get to connect:{url}. All repeats.");
    }


    private static void SaveConfigData(string text)
    {
        var doc = XDocument.Parse(text);
        foreach (var element in doc.Descendants().Where(p => !p.HasElements))
        {
            var keyInt = 0;
            var keyName = element.Name.LocalName;
            while (Data.ContainsKey(keyName))
            {
                keyName = $"{element.Name.LocalName}_{keyInt++}";
            }

            Data.Add(keyName, element.Value);
        }

        isDataReady = true;
        OnDataLoaded?.Invoke();
    }

    public static bool TryGetValue(string key, out string value)
    {
        return Data.TryGetValue($"{key}{GamePrefix}{PlatformPrefix}", out value);
    }
}