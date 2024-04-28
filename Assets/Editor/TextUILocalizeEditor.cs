using System;
using System.Collections.Generic;
using UI.PiratesOfVoxel.Localization;
using UI.PiratesOfVoxel.Localize;
using UnityEditor;
// using UnityEditor.Localization.Editor;
using UnityEngine;

namespace Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Localize), true)]
    public class TextUILocalizeEditor : UnityEditor.Editor
    {
        bool _showPreview = true;
        List<string> mKeys;

        void OnEnable()
        {
            Dictionary<string, string[]> dict = Localization.dictionary;

            if (dict.Count > 0)
            {
                mKeys = new List<string>();

                foreach (KeyValuePair<string, string[]> pair in dict)
                {
                    if (pair.Key == "KEY") continue;
                    mKeys.Add(pair.Key);
                }

                mKeys.Sort(delegate(string left, string right) { return left.CompareTo(right); });
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6f);
            EditorGUIUtility.labelWidth = 80f;
            EditorGUIUtility.fieldWidth = 100f;

            GUILayout.BeginHorizontal();

            // Key not found in the localization file -- draw it as a text field
            GUILayout.BeginVertical(GUILayout.Width(22f));
            SerializedProperty sp = serializedObject.FindProperty("_key");
            EditorGUILayout.PropertyField(sp);
            GUILayout.EndVertical();

            string myKey = sp.stringValue;
            bool isPresent = (mKeys != null) && mKeys.Contains(myKey);
            GUI.color = isPresent ? Color.green : Color.red;
            GUILayout.BeginVertical(GUILayout.Width(22f));
            GUILayout.Space(2f);
            GUILayout.Label(isPresent ? "\u2714" : "\u2718", GUILayout.Height(20f));
            GUILayout.EndVertical();
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (isPresent)
            {
                string[] keys = Localization.knownLanguages;
                string[] values;

                _showPreview = EditorGUILayout.Foldout(_showPreview, "Preview");
                if (_showPreview)
                {
                    if (Selection.activeTransform)
                    {
                        if (Localization.dictionary.TryGetValue(myKey, out values))
                        {
                            for (int i = 0; i < keys.Length; ++i)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(keys[i], GUILayout.Width(70f));
                                if (GUILayout.Button(values[i], GUILayout.MinWidth(80f),
                                        GUILayout.MaxWidth(Screen.width - 110f)))

                                    // if (GUILayout.Button(values[i]))
                                {
                                    ((Localize)target).SetValue = values[i];
                                    EditorApplication.QueuePlayerLoopUpdate();
                                    GUIUtility.hotControl = 0;
                                    GUIUtility.keyboardControl = 0;
                                }

                                GUILayout.EndHorizontal();
                            }
                        }
                        else
                        {
                            GUILayout.Label("No preview available");
                        }
                    }
                }

                if (!Selection.activeTransform)
                {
                    _showPreview = false;
                }
            }
            else if (mKeys != null && !string.IsNullOrEmpty(myKey))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(80f);
                GUILayout.BeginVertical();
                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.35f);

                int matches = 0;

                for (int i = 0, imax = mKeys.Count; i < imax; ++i)
                {
                    if (mKeys[i].StartsWith(myKey, StringComparison.OrdinalIgnoreCase) || mKeys[i].Contains(myKey))
                    {
                        if (GUILayout.Button(mKeys[i] + " \u25B2", "CN CountBadge"))
                        {
                            sp.stringValue = mKeys[i];
                            GUIUtility.hotControl = 0;
                            GUIUtility.keyboardControl = 0;
                        }

                        if (++matches == 8)
                        {
                            GUILayout.Label("...and more");
                            break;
                        }
                    }
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndVertical();
                GUILayout.Space(22f);
                GUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}