using UnityEditor;
using UnityEngine;

namespace VoxelPlay
{

    [CustomEditor (typeof (ConnectedVoxel))]
    public class VoxelPlayConnectedVoxelConfigEditor : UnityEditor.Editor
    {

        SerializedProperty voxelDefinition;
        SerializedProperty config;

        void OnEnable ()
        {
            voxelDefinition = serializedObject.FindProperty ("voxelDefinition");
            config = serializedObject.FindProperty ("config");
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField (voxelDefinition);
            EditorGUILayout.HelpBox ("Specify which adjacent prefabs are connected and which action and prefabs must be used in each case.", MessageType.Info);
            EditorGUILayout.PropertyField (config, new GUIContent ("Configuration"), true);
            serializedObject.ApplyModifiedProperties ();
        }



    }

}
