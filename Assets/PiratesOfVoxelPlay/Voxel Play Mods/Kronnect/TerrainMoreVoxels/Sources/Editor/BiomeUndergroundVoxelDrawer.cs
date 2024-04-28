using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

	[CustomPropertyDrawer (typeof(BiomeUndergroundVoxel))]
	public class BiomeUndergroundVoxelDrawer : PropertyDrawer {

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
			Rect firstColumn = position;
			firstColumn.width = position.width * 0.4f;
			Rect secondColumn = position;
			secondColumn.width = position.width * 0.2f;
			secondColumn.x += firstColumn.width;
			Rect thirdColumn = secondColumn;
			thirdColumn.x += secondColumn.width;
			Rect fourthColumn = thirdColumn;
			fourthColumn.x += thirdColumn.width;
			if (prop.GetArrayIndex () == 0) {
				firstColumn.height -= lineHeight;
				secondColumn.height -= lineHeight;
				thirdColumn.height -= lineHeight;
				fourthColumn.height -= lineHeight;
				EditorGUI.LabelField (firstColumn, "Voxel Definition");
				EditorGUI.LabelField (secondColumn, "Probability");
				EditorGUI.LabelField (thirdColumn, "Min Altitude");
				EditorGUI.LabelField (fourthColumn, "Max Altitude");
				firstColumn.y += lineHeight;
				secondColumn.y += lineHeight;
				thirdColumn.y += lineHeight;
				fourthColumn.y += lineHeight;
			}
			EditorGUI.PropertyField (firstColumn, prop.FindPropertyRelative ("voxelDefinition"), GUIContent.none);
			SerializedProperty prob = prop.FindPropertyRelative ("probability");
			prob.floatValue = EditorGUI.FloatField (secondColumn, GUIContent.none, prob.floatValue);
			EditorGUI.PropertyField (thirdColumn, prop.FindPropertyRelative ("altitudeMin"), GUIContent.none);
			EditorGUI.PropertyField (fourthColumn, prop.FindPropertyRelative ("altitudeMax"), GUIContent.none);
		}
	}

}
