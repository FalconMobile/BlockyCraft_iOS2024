using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay {
				
	[CustomEditor (typeof(ItemDefinition))]
	public class VoxelPlayItemDefinitionEditor : UnityEditor.Editor {

		SerializedProperty title, category, icon, iconPrefab;
		SerializedProperty voxelType, model;
        SerializedProperty prefab, prefab2, prefab3;
        SerializedProperty useSound, canBePicked, pickMode, pickupSound, lightIntensity;
		SerializedProperty properties;

		Color titleColor;
		static GUIStyle titleLabelStyle;
		int commonPropertyIndex;


		void OnEnable () {
			titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);

			title = serializedObject.FindProperty ("title");
			category = serializedObject.FindProperty ("category");
			icon = serializedObject.FindProperty ("icon");
			iconPrefab = serializedObject.FindProperty("iconPrefab");
			voxelType = serializedObject.FindProperty ("voxelType");
			model = serializedObject.FindProperty ("model");
			prefab = serializedObject.FindProperty ("prefab");
            prefab2 = serializedObject.FindProperty("prefab2");
            prefab3 = serializedObject.FindProperty("prefab3");
			useSound = serializedObject.FindProperty ("useSound");
            canBePicked = serializedObject.FindProperty ("canBePicked");
            pickMode = serializedObject.FindProperty ("pickMode");
            pickupSound = serializedObject.FindProperty ("pickupSound");
            lightIntensity = serializedObject.FindProperty ("lightIntensity");
            properties =  serializedObject.FindProperty ("properties");
		}


		public override void OnInspectorGUI () {
			serializedObject.UpdateIfRequiredOrScript ();
			if (titleLabelStyle == null) {
				titleLabelStyle = new GUIStyle (EditorStyles.label);
			}
			titleLabelStyle.normal.textColor = titleColor;
			titleLabelStyle.fontStyle = FontStyle.Bold;
			EditorGUIUtility.labelWidth = 130;

			EditorGUILayout.Separator ();
			GUILayout.Label ("Item Properties", titleLabelStyle);
			EditorGUILayout.PropertyField (category);
			switch (category.intValue) {
			case (int)ItemCategory.Torch:
				EditorGUILayout.HelpBox ("A special item that represents a light source. Can be attached on the sides of other voxels.", MessageType.Info);
				break;
			case (int)ItemCategory.Voxel:
				EditorGUILayout.HelpBox ("An item representing a voxel. All voxels referrenced in the world definition are available as items in build mode.", MessageType.Info);
				break;
			case (int)ItemCategory.Model:
				EditorGUILayout.HelpBox ("An item representing a structure (model definition). All model definitions referrenced in the world definition are available as items in build mode.", MessageType.Info);
				break;
			case (int)ItemCategory.General:
				EditorGUILayout.HelpBox ("Any other item like weapons, crafting tools or wereables.", MessageType.Info);
				break;
			}

			EditorGUILayout.PropertyField (title);
			EditorGUILayout.PropertyField (icon);
			EditorGUILayout.PropertyField(iconPrefab);
			EditorGUILayout.PropertyField (useSound);
			if (category.intValue != (int)ItemCategory.Model) {
				EditorGUILayout.PropertyField (canBePicked);
				if (canBePicked.boolValue) {
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField (pickMode);
					EditorGUILayout.PropertyField (pickupSound);
					EditorGUI.indentLevel--;
				}
			}

			switch (category.intValue) {
			case (int)ItemCategory.Torch:
				EditorGUILayout.PropertyField (prefab);
                EditorGUILayout.PropertyField (lightIntensity);
                break;
			case (int)ItemCategory.Voxel:
				EditorGUILayout.PropertyField (voxelType);
				break;
			case (int)ItemCategory.Model:
				EditorGUILayout.PropertyField (model);
				break;
			case (int)ItemCategory.General:
				EditorGUILayout.PropertyField (prefab);
                    EditorGUILayout.PropertyField(prefab2);
                    EditorGUILayout.PropertyField(prefab3);
				break;
			}

			EditorGUILayout.PropertyField(properties, true);

			EditorGUILayout.BeginHorizontal();
			commonPropertyIndex = EditorGUILayout.Popup("Common Properties", commonPropertyIndex, ItemDefinition.commonProperties);
			if (GUILayout.Button("Add", GUILayout.Width(60))) {
				ItemDefinition id = (ItemDefinition)target;
				id.properties = id.properties.Extend(1);
                string propName = ItemDefinition.commonProperties[commonPropertyIndex];
                if ("(user defined)".Equals(propName)) {
                    propName = "";
                }
				id.properties[id.properties.Length - 1] = new ItemProperty { name = propName };
				EditorUtility.SetDirty(id);
				serializedObject.Update();
			}
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties ();

		}

	}

}
