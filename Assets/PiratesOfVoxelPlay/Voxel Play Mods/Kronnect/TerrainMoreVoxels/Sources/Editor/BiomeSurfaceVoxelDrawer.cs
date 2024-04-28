using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

	[CustomPropertyDrawer (typeof(BiomeSurfaceVoxel))]
	public class BiomeSurfaceVoxelDrawer : PropertyDrawer {

		float lineHeight;

		public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
			GUIStyle style = GUI.skin.GetStyle ("label");
			lineHeight = style.CalcHeight (label, EditorGUIUtility.currentViewWidth);
			float height = lineHeight;
			if (prop.GetArrayIndex () == 0) {
				height *= 2;
			}
			return height;
		}


		public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label) {
			position.width *= 0.5f;
			Rect firstColumn = position;
			Rect secondColumn = position;
			secondColumn.x += position.width;
			if (prop.GetArrayIndex () == 0) {
				firstColumn.height -= lineHeight;
				secondColumn.height -= lineHeight;
				EditorGUI.LabelField (firstColumn, "Voxel Definition");
				EditorGUI.LabelField (secondColumn, "Probability");
				firstColumn.y += lineHeight;
				secondColumn.y += lineHeight;
			}
			EditorGUI.PropertyField(firstColumn, prop.FindPropertyRelative ("voxelDefinition"), GUIContent.none);
			EditorGUI.PropertyField(secondColumn, prop.FindPropertyRelative ("probability"), GUIContent.none);
		}
	}

}
