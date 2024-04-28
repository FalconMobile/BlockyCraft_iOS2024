using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IslandAdventureBattleRoyale
{

    /// <summary>
    /// Editor helper: verifies that Game scene is present in the build settings (required by Mirror as it's the 'online' scene in the configuration)
    /// </summary>
    public class CheckBuildScenes : MonoBehaviour
    {
#if UNITY_EDITOR

        public Transform errorPanel;
        public Text errorText;

        void Start()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null) return;
            for (int k=0;k<scenes.Length;k++)
            {
                if (scenes[k].path.Contains("Game.unity")) return;
            }
            errorText.text = "'Lobby' and 'Game' scenes must be added to Build Settings!";
            errorPanel.gameObject.SetActive(true);
        }

#endif

    }


}