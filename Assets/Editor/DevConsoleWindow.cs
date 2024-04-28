using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelPlay;

namespace Editor
{
    public class DevConsoleWindow : EditorWindow
    {
        private const string DEV_TOOLS_MENU = "[FullHP]/Developer Tools/";
        private const string WINDOW_TITLE = "Dev Console";

        private readonly string[] _additionalScenePaths =
        {
            "Assets/Voxel Play/Demos/Demo1_World/World_Scene.unity",
        };

        private GUIStyle _defButtonStyle;
        private List<MenuTab> _tabs;
        private Vector2 _scrollPos;

        private int _currentTab;
        private long _playerId;

        private bool _inputMobile;

        private int _damageToPlayer = 0;
        private int _respawnTime = 15;


        private readonly struct MenuTab
        {
            public readonly string name;
            public readonly string description;
            public readonly Action showPanel;

            public MenuTab(string name, string description, Action showPanel)
            {
                this.name = name;
                this.description = description;
                this.showPanel = showPanel;
            }
        }


        [MenuItem(DEV_TOOLS_MENU + WINDOW_TITLE)]
        public static void ShowWindow()
        {
            DevConsoleWindow window = GetWindow<DevConsoleWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(150, 150);
        }

        public void OnGUI()
        {
            _defButtonStyle = new GUIStyle("Button") {alignment = TextAnchor.MiddleLeft};
            GUIStyle style = EditorStyles.toolbarButton;

            if (_tabs == null)
            {
                _tabs = new List<MenuTab>
                {
                    new MenuTab("Scenes", "All scenes in project", PanelShowScenes),
                    new MenuTab("Extra", "Extra Panel", PanelExtra)
                };
            }

            string[] names = _tabs.Select(tuple => tuple.name).ToArray();

            _currentTab = GUILayout.Toolbar(_currentTab, names, style, GUI.ToolbarButtonSize.FitToContents);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            {
                WrapVerticalBox(_tabs[_currentTab]);
            }
            GUILayout.EndScrollView();
        }

        #region Panels

        private void PanelShowScenes()
        {
            string[] mainScenePaths = EditorBuildSettings.scenes.Select(x => x.path).ToArray();
            ShowScenesButtons(mainScenePaths);

            ShowTabDescriptionLabel(" Additional scenes (if needed):");
            ShowScenesButtons(_additionalScenePaths);
        }

        private void PanelExtra()
        {
            ShowTabDescriptionLabel(" Platform Interactions:");

            _inputMobile = GUILayout.Toggle(_inputMobile, " Enable mobile input system");
            VoxelPlayEnvironment.devToolsEnableMobileInput = _inputMobile;
            EditorGUILayout.Space();

            if (GUILayout.Button($"Destroy  all mob enteties", _defButtonStyle))
            {
                var networkMobs = FindObjectsOfType<NetworkMob>();
                foreach (var mob in networkMobs)
                {
                    Destroy(mob.gameObject);
                }
            }

            EditorGUILayout.Space();

            ShowTabDescriptionLabel(" Player Interactions:");

            _damageToPlayer = EditorGUILayout.IntSlider(_damageToPlayer, 0, 150);

            if (GUILayout.Button($"Deal ^that^ damage to player", _defButtonStyle))
            {
                var player = FindObjectOfType<NetworkPlayer>().gameObject;
                var damageTaker = player.GetComponentInChildren<NetworkDamageTaker>();

                WorldState.Instance.CharacterGetDamage(damageTaker, null, _damageToPlayer);
            }

            EditorGUILayout.Space();

            ShowTabDescriptionLabel(" Change player respawn time:");
            _respawnTime = EditorGUILayout.IntSlider(_respawnTime, 0, 30);
            NetworkPlayer.devRespawnTime = _respawnTime;
        }

        #endregion

        #region Helper Stuff

        private void WrapVerticalBox(MenuTab menuTab)
        {
            GUILayout.BeginVertical("Box");
            {
                ShowTabDescriptionLabel(menuTab.description, 13);
                menuTab.showPanel?.Invoke();
            }
            GUILayout.EndVertical();
        }

        private void ShowTabDescriptionLabel(string text, int fontsize = 11)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.label)
            {
                fontSize = fontsize,
            });
        }

        private void ShowScenesButtons(string[] scenePaths)
        {
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePaths[i]);

                if (GUILayout.Button($"{i}: {sceneName}", _defButtonStyle))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scenePaths[i]);
                    }
                }
            }
        }

        #endregion
    }
}