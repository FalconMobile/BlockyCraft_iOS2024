using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public class FullHPEditor : EditorWindow
{
    private const string FULL_HP = "[FullHP]/";
    private const string SUB_MENU_DEFINE = "Defines/";

    private static string DEBUG_DEFINE = "CHEAT";
    private static bool _isDebug = false;

    private static string FIREBASE_ANALYTIC_DEFINE = "FIREBASE_ANALYTIC";
    private static bool _isFirebaseAnalytic = false;

#if UNITY_EDITOR
    private const string SUB_MENU_SET_PASSWORD = "/SetPassword";
    private const string PASSWORD = "";
    private const string KEYSTORE_PASSWORD = "";
    
#endif
    private const string MENU_PREFIX = "AllScenes/";
    private const string SCENE_DEFAULT_PATH = "Assets/PiratesOfVoxelPlay/Game/Scenes";

    private const string DUST = "Game";
    private const string POOL = "Lobby";

    [MenuItem(FULL_HP + MENU_PREFIX + DUST, false, 100)]
    public static void Dust() => OpenScene(DUST);

    [MenuItem(FULL_HP + MENU_PREFIX + POOL, false, 110)]
    public static void Pool() => OpenScene(POOL);

    [MenuItem(FULL_HP + "DeleteAllPrefs")]
    public static void Clear()
    {
        PlayerPrefs.DeleteAll();
    }

    private static void OpenMenu(string sceneName)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene($"{sceneName}.unity");
    }

    private static void OpenScene(string sceneName)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene($"{SCENE_DEFAULT_PATH}/{sceneName}.unity");
    }

    [InitializeOnLoad]
    private class StartApp
    {
        static StartApp()
        {
            SetPassword();
            _isDebug = ContainsDefine(DEBUG_DEFINE);
            _isFirebaseAnalytic = ContainsDefine(FIREBASE_ANALYTIC_DEFINE);
        }
    }

    [MenuItem(FULL_HP + SUB_MENU_SET_PASSWORD)]
    private static void SetPassword()
    {
        PlayerSettings.keyaliasPass = PASSWORD;
        PlayerSettings.keystorePass = KEYSTORE_PASSWORD;
    }

    #region cheat_release

    private const string DEBUG_MENU = FULL_HP + SUB_MENU_DEFINE + nameof(SwitchDebug);

    [MenuItem(DEBUG_MENU)]
    private static void SwitchDebug()
    {
        _isDebug = !_isDebug;

        if (_isDebug)
        {
            AddNewDefine(DEBUG_DEFINE);
        }
        else
        {
            RemoveDefine(DEBUG_DEFINE);
        }
    }

    [MenuItem(DEBUG_MENU, true)]
    private static bool SwitchCheatValidation()
    {
        Menu.SetChecked(DEBUG_MENU, ContainsDefine(DEBUG_DEFINE));
        return true;
    }
    #endregion
    
    #region firebase_analytic_release
    
    private const string FIREBASE_ANALYTIC_MENU = FULL_HP + SUB_MENU_DEFINE + nameof(SwitchFirebaseAnalytic);
    
    [MenuItem(FIREBASE_ANALYTIC_MENU)]
    private static void SwitchFirebaseAnalytic()
    {
        _isFirebaseAnalytic = !_isFirebaseAnalytic;

        if (_isFirebaseAnalytic)
        {
            AddNewDefine(FIREBASE_ANALYTIC_DEFINE);
        }
        else
        {
            RemoveDefine(FIREBASE_ANALYTIC_DEFINE);
        }
    }
    
    [MenuItem(FIREBASE_ANALYTIC_MENU, true)]
    private static bool SwitchFirebaseAnalyticValidation()
    {
        Menu.SetChecked(FIREBASE_ANALYTIC_MENU, ContainsDefine(FIREBASE_ANALYTIC_DEFINE));
        return true;
    }
    
    #endregion

    private static void AddNewDefine(string newDefine)
    {
        var defineString = GetDefineString();

        if (!ContainsDefine(newDefine))
        {
            string newDefineString = string.Format("{0};{1};", defineString, newDefine);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(GetGroup(), newDefineString);
        }
    }

    private static void RemoveDefine(string defineToRemoved)
    {
        var defineString = GetDefineString();
        var defineArray = defineString.Split(';');
        string newDefineString = string.Empty;
        foreach (var define in defineArray)
        {
            if (!define.Equals(defineToRemoved))
            {
                if (newDefineString.Equals(string.Empty))
                {
                    newDefineString = $"{define};";
                }
                else
                {
                    newDefineString = $"{newDefineString}{define};";
                }
            }
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(GetGroup(), newDefineString);
    }

    private static bool ContainsDefine(string define)
    {
        var defineString = GetDefineString();
        return defineString.Contains(define);
    }

    private static string GetDefineString()
    {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(GetGroup());
    }


    private static BuildTargetGroup GetGroup()
    {
        var group =
#if UNITY_ANDROID
            BuildTargetGroup.Android;
#elif UNITY_IOS
            BuildTargetGroup.iOS;
#elif UNITY_WEBGL
            BuildTargetGroup.WebGL;
#else
            BuildTargetGroup.Unknown;
#endif

        return group;
    }
}