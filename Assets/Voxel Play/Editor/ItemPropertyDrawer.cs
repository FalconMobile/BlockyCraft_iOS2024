using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VoxelPlay {

	[CustomPropertyDrawer (typeof(ItemProperty))]
	public class ItemPropertyDrawer : PropertyDrawer {

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
			secondColumn.x += firstColumn.width;
			secondColumn.width = position.width * 0.4f;
			Rect thirdColumn = secondColumn;
			thirdColumn.x += secondColumn.width + 10;
			thirdColumn.width = position.width * 0.2f - 10;
			if (prop.GetArrayIndex () == 0) {
				firstColumn.height -= lineHeight;
				secondColumn.height -= lineHeight;
				thirdColumn.height -= lineHeight;
				EditorGUI.LabelField (firstColumn, "Property");
				EditorGUI.LabelField (secondColumn, "Value");
				firstColumn.y += lineHeight;
				secondColumn.y += lineHeight;
				thirdColumn.y += lineHeight;
			}
			EditorGUI.PropertyField(firstColumn, prop.FindPropertyRelative ("name"), GUIContent.none);
			EditorGUI.PropertyField(secondColumn, prop.FindPropertyRelative ("value"), GUIContent.none);
			if (GUI.Button(thirdColumn, "Remove"))
            {
				ItemDefinition id = (ItemDefinition)prop.serializedObject.targetObject;
				List<ItemProperty> od = new List<ItemProperty>(id.properties);
				int index = prop.GetArrayIndex();
				od.RemoveAt(index);
				id.properties = od.ToArray();
				if (!Application.isPlaying)
				{
					EditorUtility.SetDirty(id);
				}
			}
		}
	}

}
