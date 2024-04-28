using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay
{
    [CustomPropertyDrawer (typeof (ConnectedVoxelConfig))]
    public class VoxelPlayConnectedVoxelConfigDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {

            float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            const float w = 80f;
            const float sw = 80f;

            position.y += 6;

            float previewHeight = GetPropertyHeight (property, label);

            float editorWidth = position.width;
            Rect box = new Rect (position.x - 2f, position.y, editorWidth, previewHeight);
            GUI.Box (box, GUIContent.none);

            position.y += 4;
            position.height = lineHeight;
            position.width = w;

            EditorGUI.BeginChangeCheck ();

            Rect prevPosition = position;

            SerializedProperty tl = property.FindPropertyRelative ("tl");
            EditorGUI.PropertyField (position, tl, GUIContent.none);
            position.x += sw;
            SerializedProperty t = property.FindPropertyRelative ("t");
            EditorGUI.PropertyField (position, t, GUIContent.none);
            position.x += sw;
            SerializedProperty tr = property.FindPropertyRelative ("tr");
            EditorGUI.PropertyField (position, tr, GUIContent.none);

            position.x = editorWidth - position.width;
            if (GUI.Button (position, "Delete")) {
                if (EditorUtility.DisplayDialog ("", "Delete this entry?", "Yes", "No")) {
                    ConnectedTexture ct = (ConnectedTexture)property.serializedObject.targetObject;
                    List<ConnectedTextureConfig> od = new List<ConnectedTextureConfig> (ct.config);
                    int index = property.GetArrayIndex ();
                    od.RemoveAt (index);
                    ct.config = od.ToArray ();
                    GUI.changed = true;
                }
            }


            position = prevPosition;
            position.y += lineHeight + 2f;
            prevPosition = position;

            SerializedProperty l = property.FindPropertyRelative ("l");
            EditorGUI.PropertyField (position, l, GUIContent.none);
            position.x += sw;

            GUI.Label (position, "     (Center)");

            position.x += sw;
            SerializedProperty r = property.FindPropertyRelative ("r");
            EditorGUI.PropertyField (position, r, GUIContent.none);

            position = prevPosition;
            position.y += lineHeight + 2f;
            prevPosition = position;

            SerializedProperty bl = property.FindPropertyRelative ("bl");
            EditorGUI.PropertyField (position, bl, GUIContent.none);
            position.x += sw;
            SerializedProperty b = property.FindPropertyRelative ("b");
            EditorGUI.PropertyField (position, b, GUIContent.none);
            position.x += sw;
            SerializedProperty br = property.FindPropertyRelative ("br");
            EditorGUI.PropertyField (position, br, GUIContent.none);

            position = prevPosition;
            position.y += lineHeight + 2f;
            EditorGUIUtility.labelWidth = 110;
            position.width = 350;

            SerializedProperty action = property.FindPropertyRelative ("action");
            EditorGUI.PropertyField (position, action, new GUIContent("Action"));
            position.y += lineHeight + 2f;

            switch(action.intValue) {
            case (int)ConnectedVoxelConfigAction.Replace:
                SerializedProperty replacementModel = property.FindPropertyRelative ("replacementVoxelDefinition");
                EditorGUI.PropertyField (position, replacementModel, new GUIContent("Replace With"));
                break;
            case (int)ConnectedVoxelConfigAction.Cycle: case (int)ConnectedVoxelConfigAction.Random:
                SerializedProperty replacementModelSet = property.FindPropertyRelative ("replacementVoxelDefinitionSet");
                EditorGUI.PropertyField (position, replacementModelSet, new GUIContent ("Replace With"), true);
                break;
            }

            if ((EditorGUI.EndChangeCheck () || GUI.enabled) && !Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEngine.SceneManagement.SceneManager.GetActiveScene ());
            }
        }

        bool Toggle (Rect position, bool value)
        {
            GUIStyle style = new GUIStyle ("Button");
            style.onActive = style.onNormal;
            style.onFocused = style.onNormal;
            style.onHover = style.onNormal;
            style.active = style.onNormal;
            return EditorGUI.Toggle (position, GUIContent.none, value, style);
        }

        public override float GetPropertyHeight (SerializedProperty prop, GUIContent label)
        {
            int lines = 6;
            SerializedProperty action = prop.FindPropertyRelative ("action");
            if (action.intValue == (int)ConnectedVoxelConfigAction.Replace) {
                lines ++;
            } else if (action.intValue == (int)ConnectedVoxelConfigAction.Cycle || action.intValue == (int)ConnectedVoxelConfigAction.Random) {
                SerializedProperty reps = prop.FindPropertyRelative ("replacementVoxelDefinitionSet");
                lines += reps.arraySize + 2;
            }
            return EditorGUIUtility.singleLineHeight * lines;
        }

    }
}