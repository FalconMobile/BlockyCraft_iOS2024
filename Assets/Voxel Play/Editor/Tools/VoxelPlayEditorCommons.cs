using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay
{

    public static class VoxelPlayEditorCommons
    {

        public static void CheckImportSettings (Texture texture, bool requireAlphaTexture, bool forcePointFilterMode)
        {
            if (texture == null)
                return;

            string fullPath = AssetDatabase.GetAssetPath (texture);
            if (string.IsNullOrEmpty (fullPath))
                return;

            TextureImporter importerSettings = AssetImporter.GetAtPath (fullPath) as TextureImporter;
            if (importerSettings != null) {
                bool hasChanges = false;
                if (!importerSettings.isReadable) {
                    importerSettings.isReadable = true;
                    hasChanges = true;
                }
                if (forcePointFilterMode && importerSettings.filterMode != FilterMode.Point) {
                    importerSettings.filterMode = FilterMode.Point;
                    importerSettings.mipmapEnabled = false;
                    hasChanges = true;
                }
                if (requireAlphaTexture && !importerSettings.alphaIsTransparency) {
                    importerSettings.alphaIsTransparency = true;
                    hasChanges = true;
                }
                if (hasChanges) {
                    importerSettings.SaveAndReimport ();
                }
            }
        }

    }

}
