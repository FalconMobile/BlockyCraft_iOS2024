using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelPlayBehaviour))]
	public class VoxelPlayBehaviourEditor : UnityEditor.Editor {

        SerializedProperty enableVoxelLight, useVoxelPlayMaterials;
        SerializedProperty forceUnstuck, unstuckOffsetY;
		SerializedProperty checkNearChunks, chunkExtents, renderChunks;
		SerializedProperty useOriginShift;

		void OnEnable () {
			enableVoxelLight = serializedObject.FindProperty ("enableVoxelLight");
			useVoxelPlayMaterials = serializedObject.FindProperty("useVoxelPlayMaterials");
			forceUnstuck = serializedObject.FindProperty ("forceUnstuck");
            unstuckOffsetY = serializedObject.FindProperty("unstuckOffsetY");
			checkNearChunks = serializedObject.FindProperty ("checkNearChunks");
			chunkExtents = serializedObject.FindProperty ("chunkExtents");
			renderChunks = serializedObject.FindProperty ("renderChunks");
			useOriginShift = serializedObject.FindProperty ("useOriginShift");
		}


		public override void OnInspectorGUI () {
			serializedObject.Update ();
			EditorGUILayout.Separator ();
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (enableVoxelLight, new GUIContent("Enable Voxel Light", "Enable this property to adjust material lighting based on voxel global illumination"));
			EditorGUILayout.PropertyField(useVoxelPlayMaterials, new GUIContent("Use Voxel Play Materials", "Replace materials of this gameobject by optimized Voxel Play materials."));
			EditorGUILayout.PropertyField (forceUnstuck, new GUIContent("Force Unstuck", "Moves this gameobject to the surface of the terrain if it falls below or crosses a solid voxel"));
            if (forceUnstuck.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(unstuckOffsetY, new GUIContent("Offset Y"));
                EditorGUI.indentLevel--;
            }
			EditorGUILayout.PropertyField (checkNearChunks, new GUIContent("Chunk Area", "Ensures all nearby chunks are generated"));
			if (checkNearChunks.boolValue) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField (chunkExtents, new GUIContent("Extents", "Distance in chunks around the transform position (1 chunk = 16 world units by default)"));
				EditorGUILayout.PropertyField (renderChunks, new GUIContent("Render Chunks", "If this option is enabled, chunks within area will also be rendered. If this option is disabled, chunks will only be generated but no mesh/collider/navmesh will be generated."));
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.PropertyField (useOriginShift);
			serializedObject.ApplyModifiedProperties ();
			VoxelPlayBehaviour b = (VoxelPlayBehaviour)target;
			if (EditorGUI.EndChangeCheck ()) {
				b.Refresh ();
			}
            if (GUILayout.Button("Select Chunk")) {
				VoxelChunk chunk = VoxelPlayEnvironment.instance.GetChunk(b.transform.position);
                if (chunk != null) {
					chunk.gameObject.hideFlags = 0;
					Selection.activeGameObject = chunk.gameObject;
					EditorGUIUtility.PingObject(chunk.gameObject);
                }
            }
		}
	}

}
