using UnityEngine;
using UnityEditor;

namespace VoxelPlay
{

    [CustomEditor (typeof (VoxelDefinition))]
    public class VoxelPlayVoxelDefinitionEditor : UnityEditor.Editor
    {

        SerializedProperty title, renderType, opaque;
        SerializedProperty overrideMaterial, texturesByMaterial, overrideMaterialNonGeo;
        SerializedProperty texturesCustomPacking, texturesPackingSize, texturesPackingScale, texturesPackingNormalMap, texturesPackingReliefMap;
        SerializedProperty textureTop, textureTopEmission, textureTopNRM, textureTopDISP;
        SerializedProperty textureSide, textureSideEmission, textureSideNRM, textureSideDISP;
        SerializedProperty textureRight, textureRightEmission, textureRightNRM, textureRightDISP;
        SerializedProperty textureForward, textureForwardEmission, textureForwardNRM, textureForwardDISP;
        SerializedProperty textureLeft, textureLeftEmission, textureLeftNRM, textureLeftDISP;
        SerializedProperty textureBottom, textureBottomEmission, textureBottomNRM, textureBottomDISP;
        SerializedProperty showFoam, tintColor, colorVariation, alpha;
        SerializedProperty pickupSound, buildSound, footfalls, jumpSound, landingSound, impactSound, destructionSound;
        SerializedProperty showDamageCracks, resistancePoints, canBeCollected, hidden, dropItem, dropItemLifeTime, dropItemScale;
        SerializedProperty icon, textureSample, overrideMainTexture, overrideMainTextureOffset, triggerCollapse, willCollapse, navigatable, denseLeaves, windAnimation;
        SerializedProperty model, prefabMaterial, gpuInstancing, castShadows, receiveShadows, createGameObject;
        SerializedProperty offset, offsetRandom, offsetRandomRange, scale, rotation, rotationRandomY, promotesTo, replacedBy;
        SerializedProperty spreads, drains, spreadDelay, spreadDelayRandom, spreadReplaceThreshold, supportsBevel, diveColor, height;
        SerializedProperty playerDamage, playerDamageDelay, ignoresRayCast, highlightOffset;
        SerializedProperty placeOnWall, placeFacingPlayer, allowsTextureRotation;
        SerializedProperty triggerEnterEvent, triggerWalkEvent;
        SerializedProperty biomeDirtCounterpart, seeThroughMode, seeThroughVoxel;
        SerializedProperty animationSpeed, animationTextures;
        SerializedProperty generateColliders, lightIntensity;
        SerializedProperty allowUpsideDownVoxel, upsideDownVoxel, isUpsideDown;

        GUIContent [] renderTypesNames = {
            new GUIContent ("Opaque 3 Textures (no ambient occlusion)"),
            new GUIContent ("Opaque 3 Textures (with ambient occlusion)"),
            new GUIContent ("Opaque 3 Textures (with ambient occlusion and animation)"),
            new GUIContent ("Opaque 6 Textures (with ambient occlusion)"),
            new GUIContent ("Transparent 6 textures"),
            new GUIContent ("Cutout"),
            new GUIContent ("Water"),
            new GUIContent ("Cloud (scaled x4, no AO, no collider)"),
            new GUIContent ("Vegetation (Cutout Cross)"),
            new GUIContent ("Custom (prefab)"),
            new GUIContent ("Invisible")
        };

        int [] renderTypesValues = {
            (int)RenderType.OpaqueNoAO,
            (int)RenderType.Opaque,
            (int)RenderType.OpaqueAnimated,
            (int)RenderType.Opaque6tex,
            (int)RenderType.Transp6tex,
            (int)RenderType.Cutout,
            (int)RenderType.Water,
            (int)RenderType.Cloud,
            (int)RenderType.CutoutCross,
            (int)RenderType.Custom,
            (int)RenderType.Invisible
        };

        Color titleColor;
        static GUIStyle titleLabelStyle;
        VoxelPlayEnvironment _env;

        void OnEnable ()
        {
            titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);

            title = serializedObject.FindProperty ("title");
            renderType = serializedObject.FindProperty ("renderType");
            overrideMaterial = serializedObject.FindProperty ("overrideMaterial");
            overrideMaterialNonGeo = serializedObject.FindProperty ("overrideMaterialNonGeo");
            texturesByMaterial = serializedObject.FindProperty ("texturesByMaterial");

            texturesCustomPacking = serializedObject.FindProperty ("texturesCustomPacking");
            texturesPackingSize = serializedObject.FindProperty ("texturesPackingSize");
            texturesPackingScale = serializedObject.FindProperty ("texturesPackingScale");
            texturesPackingNormalMap = serializedObject.FindProperty ("texturesPackingNormalMap");
            texturesPackingReliefMap = serializedObject.FindProperty ("texturesPackingReliefMap");

            opaque = serializedObject.FindProperty ("opaque");
            textureTop = serializedObject.FindProperty ("textureTop");
            textureTopEmission = serializedObject.FindProperty ("textureTopEmission");
            textureTopNRM = serializedObject.FindProperty ("textureTopNRM");
            textureTopDISP = serializedObject.FindProperty ("textureTopDISP");
            textureSide = serializedObject.FindProperty ("textureSide");
            textureSideEmission = serializedObject.FindProperty ("textureSideEmission");
            textureSideNRM = serializedObject.FindProperty ("textureSideNRM");
            textureSideDISP = serializedObject.FindProperty ("textureSideDISP");
            textureRight = serializedObject.FindProperty ("textureRight");
            textureRightEmission = serializedObject.FindProperty ("textureRightEmission");
            textureRightNRM = serializedObject.FindProperty ("textureRightNRM");
            textureRightDISP = serializedObject.FindProperty ("textureRightDISP");
            textureForward = serializedObject.FindProperty ("textureForward");
            textureForwardEmission = serializedObject.FindProperty ("textureForwardEmission");
            textureForwardNRM = serializedObject.FindProperty ("textureForwardNRM");
            textureForwardDISP = serializedObject.FindProperty ("textureForwardDISP");
            textureLeft = serializedObject.FindProperty ("textureLeft");
            textureLeftEmission = serializedObject.FindProperty ("textureLeftEmission");
            textureLeftNRM = serializedObject.FindProperty ("textureLeftNRM");
            textureLeftDISP = serializedObject.FindProperty ("textureLeftDISP");
            textureBottom = serializedObject.FindProperty ("textureBottom");
            textureBottomEmission = serializedObject.FindProperty ("textureBottomEmission");
            textureBottomNRM = serializedObject.FindProperty ("textureBottomNRM");
            textureBottomDISP = serializedObject.FindProperty ("textureBottomDISP");

            showFoam = serializedObject.FindProperty ("showFoam");
            tintColor = serializedObject.FindProperty ("tintColor");
            colorVariation = serializedObject.FindProperty ("colorVariation");
            alpha = serializedObject.FindProperty ("alpha");

            pickupSound = serializedObject.FindProperty ("pickupSound");
            buildSound = serializedObject.FindProperty ("buildSound");
            footfalls = serializedObject.FindProperty ("footfalls");
            jumpSound = serializedObject.FindProperty ("jumpSound");
            landingSound = serializedObject.FindProperty ("landingSound");
            impactSound = serializedObject.FindProperty ("impactSound");
            destructionSound = serializedObject.FindProperty ("destructionSound");
            resistancePoints = serializedObject.FindProperty ("resistancePoints");
            showDamageCracks = serializedObject.FindProperty ("showDamageCracks");

            canBeCollected = serializedObject.FindProperty ("canBeCollected");
            hidden = serializedObject.FindProperty ("hidden");
            dropItem = serializedObject.FindProperty ("dropItem");
            dropItemLifeTime = serializedObject.FindProperty ("dropItemLifeTime");
            dropItemScale = serializedObject.FindProperty ("dropItemScale");
            icon = serializedObject.FindProperty ("icon");
            textureSample = serializedObject.FindProperty ("textureSample");
            overrideMainTexture = serializedObject.FindProperty ("overrideMainTexture");
            overrideMainTextureOffset = serializedObject.FindProperty ("overrideMainTextureOffset");
            navigatable = serializedObject.FindProperty ("navigatable");
            denseLeaves = serializedObject.FindProperty ("denseLeaves");
            windAnimation = serializedObject.FindProperty ("windAnimation");
            model = serializedObject.FindProperty ("model");
            prefabMaterial = serializedObject.FindProperty ("prefabMaterial");
            gpuInstancing = serializedObject.FindProperty ("gpuInstancing");
            castShadows = serializedObject.FindProperty ("castShadows");
            receiveShadows = serializedObject.FindProperty ("receiveShadows");
            createGameObject = serializedObject.FindProperty ("createGameObject");
            offset = serializedObject.FindProperty ("offset");
            offsetRandom = serializedObject.FindProperty ("offsetRandom");
            offsetRandomRange = serializedObject.FindProperty ("offsetRandomRange");
            scale = serializedObject.FindProperty ("scale");
            rotation = serializedObject.FindProperty ("rotation");
            rotationRandomY = serializedObject.FindProperty ("rotationRandomY");
            promotesTo = serializedObject.FindProperty ("promotesTo");
            replacedBy = serializedObject.FindProperty ("replacedBy");
            triggerCollapse = serializedObject.FindProperty ("triggerCollapse");
            willCollapse = serializedObject.FindProperty ("willCollapse");

            spreads = serializedObject.FindProperty ("spreads");
            drains = serializedObject.FindProperty ("drains");
            spreadDelay = serializedObject.FindProperty ("spreadDelay");
            spreadDelayRandom = serializedObject.FindProperty ("spreadDelayRandom");
            spreadReplaceThreshold = serializedObject.FindProperty ("spreadReplaceThreshold");

            supportsBevel = serializedObject.FindProperty ("supportsBevel");
            diveColor = serializedObject.FindProperty ("diveColor");
            height = serializedObject.FindProperty ("height");

            playerDamage = serializedObject.FindProperty ("playerDamage");
            playerDamageDelay = serializedObject.FindProperty ("playerDamageDelay");
            ignoresRayCast = serializedObject.FindProperty ("ignoresRayCast");
            generateColliders = serializedObject.FindProperty ("generateColliders");
            highlightOffset = serializedObject.FindProperty ("highlightOffset");

            allowsTextureRotation = serializedObject.FindProperty ("allowsTextureRotation");
            placeOnWall = serializedObject.FindProperty ("placeOnWall");
            placeFacingPlayer = serializedObject.FindProperty ("placeFacingPlayer");

            triggerEnterEvent = serializedObject.FindProperty ("triggerEnterEvent");
            triggerWalkEvent = serializedObject.FindProperty ("triggerWalkEvent");

            biomeDirtCounterpart = serializedObject.FindProperty ("biomeDirtCounterpart");
            animationSpeed = serializedObject.FindProperty ("animationSpeed");
            animationTextures = serializedObject.FindProperty ("animationTextures");

            seeThroughMode = serializedObject.FindProperty ("seeThroughMode");
            seeThroughVoxel = serializedObject.FindProperty ("seeThroughVoxel");

            lightIntensity = serializedObject.FindProperty ("lightIntensity");

            allowUpsideDownVoxel = serializedObject.FindProperty ("allowUpsideDownVoxel");
            upsideDownVoxel = serializedObject.FindProperty ("upsideDownVoxel");
            isUpsideDown = serializedObject.FindProperty ("isUpsideDown");
        }


        public override void OnInspectorGUI ()
        {
#if UNITY_5_6_OR_NEWER
            serializedObject.UpdateIfRequiredOrScript ();
#else
			serializedObject.UpdateIfDirtyOrScript();
#endif
            if (titleLabelStyle == null) {
                titleLabelStyle = new GUIStyle (EditorStyles.label);
            }
            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;
            EditorGUIUtility.labelWidth = 130;

            EditorGUILayout.Separator ();
            GUILayout.Label ("Rendering", titleLabelStyle);
            EditorGUILayout.IntPopup (renderType, renderTypesNames, renderTypesValues);
            RenderType rt = (RenderType)renderType.intValue;
            EditorGUILayout.HelpBox (GetRenderTypeDescription (rt), MessageType.Info);

            if (rt != RenderType.Custom && rt != RenderType.Invisible) {
                EditorGUILayout.PropertyField (texturesCustomPacking, new GUIContent ("Custom Packing", "Specifies custom texture packing settings for this voxel definition, including custom texture size, normal map and relief mapping support."));
                if (texturesCustomPacking.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox ("Custom texture packing allows you to use textures of different sizes for this voxel definition (and custom normal/relief map even if regular voxels do not use them). All textures provided below will be packed in a different texture array. Voxel Play will reuse the same texture array for textures with same size, uv scale and normal/relief mapping settings. Try to use a less different combinations as possible across all voxel definitions.", MessageType.Info);
                    EditorGUILayout.PropertyField (texturesPackingSize, new GUIContent ("Texture Size", "Texture size for each individual texture used by this material. If some textures are of different sizes, they'll be rescaled to this size. All textures with same size are then packed into a single texture array used by this material."));
                    EditorGUILayout.PropertyField (texturesPackingScale, new GUIContent ("UV Scale", "UV multiplier."));
                    EditorGUILayout.PropertyField (texturesPackingNormalMap, new GUIContent ("Enable Normal Map", "Enables or disables normal map effect for this material."));
                    EditorGUILayout.PropertyField (texturesPackingReliefMap, new GUIContent ("Enable Relief Map", "Enables or disables relief mapping effect for this material."));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField (overrideMaterial);
                if (overrideMaterial.boolValue) {
                    EditorGUILayout.HelpBox ("Material shader must be compatible with the original VP shader.\nCheck the online documentation for more details.", MessageType.Info);
                    EditorGUILayout.BeginHorizontal ();
                    EditorGUILayout.PropertyField (overrideMaterialNonGeo, new GUIContent ("Material", "Overriding material."));
                    if (GUILayout.Button ("Locate Original")) {
                        LocateOriginal (rt);
                    }
                    EditorGUILayout.EndHorizontal ();
                    EditorGUILayout.PropertyField (texturesByMaterial);
                    if (texturesByMaterial.boolValue) {
                        EditorGUILayout.HelpBox ("Shaders with name 'Voxel Play/Voxels/Override Examples/*** are examples you can use or duplicate.", MessageType.Info);
                    }
                }
            }

            bool showTextureFields = !overrideMaterial.boolValue || !texturesByMaterial.boolValue;
            if (textureSample.objectReferenceValue == null && textureSide.objectReferenceValue != null) {
                textureSample.objectReferenceValue = textureSide.objectReferenceValue;
            }
            bool requiresAlphaTexture = rt == RenderType.Transp6tex || rt == RenderType.Cutout || rt == RenderType.CutoutCross || rt == RenderType.Water;

            switch (rt) {
            case RenderType.Custom:
                EditorGUILayout.PropertyField (model, new GUIContent ("Prefab", "Assign a prefab. Make sure your prefab uses a valid material (you can copy one of the VP Model * materials provided with Voxel Play). Please check the documentation for details."));
                EditorGUILayout.PropertyField (prefabMaterial, new GUIContent ("Material", "The material to use when rendering the custom voxel. You can use the material provided by the prefab or use one of the optimized materials provided by Voxel Play."));
                if (prefabMaterial.intValue != (int)CustomVoxelMaterial.PrefabMaterial) {
                    EditorGUILayout.HelpBox ("Existing material will be replaced at runtime by this one - previous material color, texture and normal maps will be used.", MessageType.Info);
                }
                EditorGUILayout.PropertyField (offset);
                EditorGUILayout.PropertyField (offsetRandom);
                if (offsetRandom.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (offsetRandomRange, new GUIContent ("Offset Range", "Scale applied to random on each axis."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField (scale);
                EditorGUILayout.PropertyField (rotation);
                EditorGUILayout.PropertyField (rotationRandomY);
                TextureField (textureSample, false, "Texture Sample", "Texture that represents the object colors. Used for sampling particle colors and inventory.");
                EditorGUILayout.PropertyField (overrideMainTexture);
                if (overrideMainTexture.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (overrideMainTextureOffset, new GUIContent ("Texture Offset", "Offsets the overridden texture"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField (opaque, new GUIContent ("Opaque", "Set this value to 15 to specify that this is a fully solid object that occludes other adjacent voxels. A lower value let light pass through and reduces it by this amount. 0 = fully transparent."));
                EditorGUILayout.PropertyField (gpuInstancing, new GUIContent ("GPU Instancing", "Uses GPU instancing to render the model."));
                if (gpuInstancing.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (castShadows, new GUIContent ("Cast Shadows", "If this instanced voxel can cast shadows."));
                    EditorGUILayout.PropertyField (receiveShadows, new GUIContent ("Receive Shadows", "If this instanced voxel can cast shadows."));
                    EditorGUILayout.PropertyField (createGameObject, new GUIContent ("Create GameObject", "When GPU instancing is enabled, the rendering will be done in GPU but you can still force the creation of a gameobject which can hold colliders or custom scripts."));
                    EditorGUI.indentLevel--;
                }
                break;
            case RenderType.OpaqueNoAO:
            case RenderType.Opaque:
            case RenderType.OpaqueAnimated:
            case RenderType.Cloud:
            case RenderType.Cutout:
            case RenderType.Water:
                if (showTextureFields) {
                    TextureField (textureTop, requiresAlphaTexture);
                    if (rt == RenderType.Opaque) {
                        EditorGUI.indentLevel++;
                        TextureField (textureTopEmission, false, "Emission Mask");
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel++;
                    TextureField (textureTopNRM, false, "Normal Map");
                    TextureField (textureTopDISP, false, "Displacement Map");
                    EditorGUI.indentLevel--;
                    TextureField (textureSide, requiresAlphaTexture);
                    if (textureSide.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureSideEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureSideNRM, false, "Normal Map");
                        TextureField (textureSideDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                    TextureField (textureBottom, requiresAlphaTexture);
                    if (textureBottom.objectReferenceValue != textureTop.objectReferenceValue && textureBottom.objectReferenceValue != textureSide.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureBottomEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureBottomNRM, false, "Normal Map");
                        TextureField (textureBottomDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                }
                if (rt == RenderType.Water) {
                    EditorGUILayout.PropertyField (showFoam);
                } else {
                    EditorGUI.BeginChangeCheck ();
                    EditorGUILayout.PropertyField (tintColor);
                    if (!EditorGUI.EndChangeCheck ()) {
                        CheckTintColorFeature ();
                    }
                }
                break;
            case RenderType.Opaque6tex:
            case RenderType.Transp6tex:
                if (showTextureFields) {
                    TextureField (textureTop, requiresAlphaTexture);
                    if (rt.supportsEmission ()) {
                        EditorGUI.indentLevel++;
                        TextureField (textureTopEmission, false, "Emission Mask");
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel++;
                    TextureField (textureTopNRM, false, "Normal Map");
                    TextureField (textureTopDISP, false, "Displacement Map");
                    EditorGUI.indentLevel--;
                    TextureField (textureBottom, requiresAlphaTexture);
                    if (textureBottom.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureBottomEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureBottomNRM, false, "Normal Map");
                        TextureField (textureBottomDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                    TextureField (textureSide, requiresAlphaTexture, "Texture Back");
                    if (textureSide.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureSideEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureSideNRM, false, "Normal Map");
                        TextureField (textureSideDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                    TextureField (textureRight, requiresAlphaTexture, "Texture Right");
                    if (textureRight.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureRightEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureRightNRM, false, "Normal Map");
                        TextureField (textureRightDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                    TextureField (textureForward, requiresAlphaTexture, "Texture Forward");
                    if (textureForward.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureForwardEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureForwardNRM, false, "Normal Map");
                        TextureField (textureForwardDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                    TextureField (textureLeft, requiresAlphaTexture, "Texture Left");
                    if (textureLeft.objectReferenceValue != textureTop.objectReferenceValue) {
                        if (rt.supportsEmission ()) {
                            EditorGUI.indentLevel++;
                            TextureField (textureLeftEmission, false, "Emission Mask");
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel++;
                        TextureField (textureLeftNRM, false, "Normal Map");
                        TextureField (textureLeftDISP, false, "Displacement Map");
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField (tintColor);
                if (!EditorGUI.EndChangeCheck ()) {
                    CheckTintColorFeature ();
                }
                if (renderType.intValue == (int)RenderType.Transp6tex) {
                    EditorGUILayout.PropertyField (alpha, new GUIContent ("Alpha", "Custom alpha for transparent voxels. Texture alpha value is multipled by this factor."));
                }
                break;
            case RenderType.CutoutCross:
                TextureField (textureSide, requiresAlphaTexture, "Texture");
                break;
            }
            if (rt == RenderType.Cutout || rt == RenderType.CutoutCross) {
                EditorGUILayout.PropertyField (colorVariation);
            } else if (rt == RenderType.OpaqueAnimated) {
                EditorGUILayout.PropertyField (animationTextures, new GUIContent ("Additional Textures"), true);
                EditorGUILayout.PropertyField (animationSpeed, new GUIContent ("Speed"));
            }

            if (rt != RenderType.Invisible) {
                EditorGUILayout.PropertyField (lightIntensity);
            }

            EditorGUILayout.Separator ();

            GUILayout.Label ("Sound Effects", titleLabelStyle);
            EditorGUILayout.PropertyField (pickupSound);
            EditorGUILayout.PropertyField (buildSound);
            if (rt != RenderType.Cutout && rt != RenderType.CutoutCross && rt != RenderType.Water) {
                EditorGUILayout.PropertyField (footfalls, true);
            }
            EditorGUILayout.PropertyField (jumpSound);
            EditorGUILayout.PropertyField (landingSound);
            EditorGUILayout.PropertyField (impactSound);
            EditorGUILayout.PropertyField (destructionSound);

            EditorGUILayout.Separator ();


            GUILayout.Label ("Special Events", titleLabelStyle);
            EditorGUILayout.PropertyField (triggerWalkEvent);
            EditorGUILayout.PropertyField (triggerEnterEvent);

            EditorGUILayout.Separator ();

            GUILayout.Label ("Inventory-related", titleLabelStyle);
            EditorGUILayout.PropertyField (title);
            EditorGUILayout.PropertyField (hidden);
            if (!hidden.boolValue) {
                EditorGUILayout.PropertyField (icon);
                EditorGUILayout.PropertyField (canBeCollected);
                if (canBeCollected.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (dropItem);
                    EditorGUILayout.PropertyField (dropItemLifeTime);
                    EditorGUILayout.PropertyField (dropItemScale);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Separator ();

            GUILayout.Label ("Placement", titleLabelStyle);

            if (rt.supportsTextureRotation ()) {
                if (rt == RenderType.Custom) {
                    EditorGUILayout.PropertyField (placeOnWall);
                }
                if (!placeOnWall.boolValue) {
                    EditorGUILayout.PropertyField (allowsTextureRotation, new GUIContent ("Can Rotate", "Allows texture/object rotation by using VoxelRotateTextures or VoxelRotate (for custom voxel) and similar methods."));
                    GUI.enabled = allowsTextureRotation.boolValue;
                    EditorGUILayout.PropertyField (placeFacingPlayer);
                    GUI.enabled = true;
                }
            }
            EditorGUILayout.PropertyField (promotesTo);

            EditorGUILayout.PropertyField (replacedBy);
            if (rt != RenderType.Cloud && rt != RenderType.Custom) {
                EditorGUILayout.PropertyField (biomeDirtCounterpart);
            }

            if (rt == RenderType.Custom && !placeOnWall.boolValue) {
                EditorGUILayout.PropertyField (allowUpsideDownVoxel, new GUIContent ("Allow Upside Down", "Allows the voxel to be placed upside down."));
                if (allowUpsideDownVoxel.boolValue) {
                    // Temporary, since GPU instancing, doesnt work with this currently
                    if (gpuInstancing.boolValue) {
                        gpuInstancing.boolValue = false;
                    }
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (isUpsideDown);
                    if (isUpsideDown.boolValue) {
                        EditorGUILayout.PropertyField (upsideDownVoxel, new GUIContent ("Normal Voxel"));
                    } else {
                        EditorGUILayout.PropertyField (upsideDownVoxel, new GUIContent ("Upside Down Voxel"));

                        if (GUILayout.Button ("Create Upside Down Voxel")) {
                            string oldpath = AssetDatabase.GetAssetPath (target);
                            string newPath = oldpath.Replace (".asset", "_Upside_Down.asset");
                            AssetDatabase.CopyAsset (oldpath, newPath);
                            VoxelDefinition newDefinition = AssetDatabase.LoadAssetAtPath (newPath, typeof (VoxelDefinition)) as VoxelDefinition;

                            upsideDownVoxel.objectReferenceValue = newDefinition;
                            isUpsideDown.boolValue = false;

                            newDefinition.promotesTo = promotesTo.objectReferenceValue as VoxelDefinition;
                            newDefinition.dropItem = dropItem.objectReferenceValue as ItemDefinition;
                            newDefinition.allowsTextureRotation = allowsTextureRotation.boolValue;
                            newDefinition.placeFacingPlayer = placeFacingPlayer.boolValue;
                            newDefinition.dropItemLifeTime = dropItemLifeTime.floatValue;
                            newDefinition.upsideDownVoxel = target as VoxelDefinition;
                            newDefinition.canBeCollected = canBeCollected.boolValue;
                            newDefinition.dropItemScale = dropItemScale.floatValue;
                            newDefinition.isUpsideDown = true;
                            newDefinition.rotation.z = -180f;
                            newDefinition.hidden = true;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Separator ();

            GUILayout.Label ("Other Attributes", titleLabelStyle);

            if (rt.supportsNavigation ()) {
                EditorGUILayout.PropertyField (navigatable);
            }

            if (rt == RenderType.Cutout) {
                EditorGUILayout.PropertyField (denseLeaves);
            }

            if (rt.supportsWindAnimation ()) {
                EditorGUILayout.PropertyField (windAnimation);
            }

            if (rt.supportsBevel ()) {
                EditorGUILayout.PropertyField (supportsBevel);
            }

            if (rt == RenderType.Water) {
                EditorGUILayout.PropertyField (height);
                EditorGUILayout.PropertyField (diveColor);
                EditorGUILayout.PropertyField (spreads);
                if (spreads.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (spreadDelay, new GUIContent ("Delay"));
                    EditorGUILayout.PropertyField (spreadDelayRandom, new GUIContent ("Randomness"));
                    EditorGUILayout.PropertyField (spreadReplaceThreshold, new GUIContent ("Replace Threshold"));
                    EditorGUILayout.PropertyField (drains);
                    EditorGUI.indentLevel--;
                }
            }
            if (rt != RenderType.CutoutCross && rt != RenderType.Water && rt != RenderType.Invisible && rt != RenderType.Cloud) {
                EditorGUILayout.PropertyField (triggerCollapse);
                EditorGUILayout.PropertyField (willCollapse);
            }

            EditorGUILayout.PropertyField (resistancePoints);
            EditorGUILayout.PropertyField (showDamageCracks);

            EditorGUILayout.PropertyField (playerDamage);
            GUI.enabled = playerDamage.intValue > 0;
            EditorGUILayout.PropertyField (playerDamageDelay);
            GUI.enabled = true;

            EditorGUILayout.PropertyField (ignoresRayCast);
            if (!ignoresRayCast.boolValue) {
                EditorGUILayout.PropertyField (highlightOffset);
            }

            if (rt.supportsOptionalColliders ()) {
                EditorGUILayout.PropertyField (generateColliders);
            }

            EditorGUILayout.PropertyField (seeThroughMode);
            if (seeThroughMode.intValue == (int)SeeThroughMode.ReplaceVoxel) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField (seeThroughVoxel, new GUIContent ("Replace By", "The voxel used to render when see-through effect occurs. This voxel can be a variation of this voxel with transparency of any other type of voxel."));
                EditorGUI.indentLevel--;
            } else if (seeThroughMode.intValue == (int)SeeThroughMode.Transparency && !rt.supportsAlphaSeeThrough ()) {
                EditorGUILayout.HelpBox ("This render type doesn't support alpha-based seethrough mode.", MessageType.Warning);
            }

            if (serializedObject.ApplyModifiedProperties ()) {
                CheckAnimationTexturesImportSettings ();
            }
        }

        void TextureField (SerializedProperty texture, bool requireAlphaTexture, string label = null, string tooltip = null)
        {
            EditorGUI.BeginChangeCheck ();
            if (label != null) {
                EditorGUILayout.PropertyField (texture, new GUIContent (label, tooltip));
            } else {
                EditorGUILayout.PropertyField (texture);
            }

            if (EditorGUI.EndChangeCheck ()) {
                if (texture.objectReferenceValue != null) {
                    Texture textureAsset = (Texture)texture.objectReferenceValue;
                    VoxelPlayEditorCommons.CheckImportSettings (textureAsset, requireAlphaTexture, false);
                }
            }
        }

        VoxelPlayEnvironment env {
            get {
                if (_env == null) {
                    _env = VoxelPlayEnvironment.instance;
                }
                return _env;
            }
        }

        void CheckTintColorFeature ()
        {
            if (tintColor.colorValue.r != 1f || tintColor.colorValue.g != 1f || tintColor.colorValue.b != 1f) {
                VoxelPlayEnvironment e = env;
                if (e != null) {
                    if (!e.enableTinting) {
                        EditorGUILayout.HelpBox ("Tint Color shader feature is disabled in Voxel Play Environment component.", MessageType.Warning);
                    }
                }
            }
        }

        void LocateOriginal (RenderType renderType)
        {
            Material mat = renderType.GetDefaultMaterial (env);
            if (mat != null) {
                EditorGUIUtility.PingObject (mat);
            } else {
                Debug.LogError ("Default material not found.");
            }
        }

        string GetRenderTypeDescription (RenderType rt)
        {
            switch (rt) {
            case RenderType.Opaque: return "A fully opaque cubic voxel which doesn't allow light to pass through. Supports 3 textures: top, bottom and a third texture for all 6 sides.";
            case RenderType.OpaqueAnimated: return "A fully opaque cubic voxel which doesn't allow light to pass through. Supports 3 textures with animation.";
            case RenderType.Opaque6tex: return "A fully opaque cubic voxel which doesn't allow light to pass through. Supports 6 textures: one texture per cube face.";
            case RenderType.OpaqueNoAO: return "A fully opaque cubic voxel which doesn't allow light to pass through. Does not support ambient occlusion nor global illumination (doesn't get dark).";
            case RenderType.Cloud: return "A render type specific for rendering clouds.";
            case RenderType.Transp6tex: return "A transparent cubic voxel. Supports 6 textures: one texture per cube face. The alpha value of the texture determines the level of transparency.";
            case RenderType.Water: return "Reserved for water rendering.";
            case RenderType.Cutout: return "A cutout cubic voxel. Mostly used for tree leaves and voxels with holes.";
            case RenderType.CutoutCross: return "Reserved for vegetation rendering. Uses two quads to render bushes.";
            case RenderType.Invisible: return "Does not render anything but can generate collider.";
            case RenderType.Custom: return "Used for custom shapes like objects, half-blocks, stylish trees or vegetation, etc. The material used by the prefab will be used for rendering (check the online documentation about custom voxels for valid materials).";
            default:
                return "Unknown render type!";
            }
        }

        void CheckAnimationTexturesImportSettings ()
        {
            VoxelDefinition vd = (VoxelDefinition)target;
            if (vd.animationTextures == null) return;
            for (int k = 0; k < vd.animationTextures.Length; k++) {
                VoxelPlayEditorCommons.CheckImportSettings (vd.animationTextures [k].textureTop, false, false);
                VoxelPlayEditorCommons.CheckImportSettings (vd.animationTextures [k].textureSide, false, false);
                VoxelPlayEditorCommons.CheckImportSettings (vd.animationTextures [k].textureBottom, false, false);
            }
        }

    }

}
