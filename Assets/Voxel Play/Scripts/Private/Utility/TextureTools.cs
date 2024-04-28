using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelPlay
{

    public static class TextureTools
    {


        readonly static Dictionary<Texture2D, Color32 []> scaledTextures = new Dictionary<Texture2D, Color32 []> ();
        static Texture2D scaledTexture;

        public static Color32 [] ScaleTextureColors (Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
        {
            Color32 [] scaledColors;
            if (scaledTextures.TryGetValue (tex, out scaledColors)) return scaledColors;

            RenderTexture currentActiveRT = RenderTexture.active;

            RenderTexture rtt = RenderTexture.GetTemporary (width, height, 0);

            if (tex.filterMode != mode) {
                tex.filterMode = mode;
                tex.Apply (true);
            }

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget (rtt);
            Graphics.Blit (tex, rtt);

            // Update new texture
            if (scaledTexture == null || scaledTexture.width != width || scaledTexture.height != height) {
                scaledTexture = new Texture2D (width, height, TextureFormat.ARGB32, false);
                scaledTexture.hideFlags = HideFlags.DontSave;
            }

            Rect texR = new Rect (0, 0, width, height);
            scaledTexture.ReadPixels (texR, 0, 0, true);
            scaledTexture.Apply (true);

            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary (rtt);

            scaledColors = scaledTexture.GetPixels32 ();
            scaledTextures [tex] = scaledColors;
            return scaledColors;
        }

        public static void Release ()
        {
            scaledTextures.Clear ();
            if (scaledTexture != null) {
                Object.DestroyImmediate (scaledTexture);
            }
        }

        public static void EnsureTextureReadable (Texture2D tex)
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath (tex);
            if (string.IsNullOrEmpty (path))
                return;
            TextureImporter imp = AssetImporter.GetAtPath (path) as TextureImporter;
            if (imp != null && !imp.isReadable) {
                imp.isReadable = true;
                imp.SaveAndReimport ();
            }
#endif
        }


        public static Texture2D GetSolidTexture (Texture2D tex)
        {
            if (tex == null)
                return tex;
            EnsureTextureReadable (tex);
            Texture2D tex2 = new Texture2D (tex.width, tex.height, TextureFormat.ARGB32, false);
            tex2.name = tex.name;
            Color32 [] colors = tex.GetPixels32 ();
            for (int k = 0; k < colors.Length; k++) {
                colors [k].a = 255;
            }
            tex2.SetPixels32 (colors);
            tex2.Apply ();
            return tex2;
        }

        public static void ScaleTexture (Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
        {
            if (tex.width == width && tex.height == height) return;
            RenderTexture currentActiveRT = RenderTexture.active;
            Rect texR = new Rect (0, 0, width, height);
            if (tex.filterMode != mode) {
                tex.filterMode = mode;
                tex.Apply (true);
            }
            RenderTexture rtt = RenderTexture.GetTemporary (width, height, 0);
            Graphics.Blit (tex, rtt);
            // Update new texture
            tex.Reinitialize (width, height, TextureFormat.ARGB32, false);
            tex.ReadPixels (texR, 0, 0, true);
            tex.Apply (true);
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary (rtt);
        }

        public static void Smooth (Texture2D tex, float smoothAmount)
        {
            int w = tex.width;
            int h = tex.height;
            int ws = Mathf.Clamp ((int)(w * (1f - smoothAmount)), 1, w);
            int hs = Mathf.Clamp ((int)(h * (1f - smoothAmount)), 1, h);
            ScaleTexture (tex, ws, hs);
            ScaleTexture (tex, w, h);
        }
    }

}