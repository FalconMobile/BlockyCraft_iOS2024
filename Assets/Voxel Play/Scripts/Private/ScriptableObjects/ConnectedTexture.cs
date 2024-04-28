using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public delegate int ConnectedTexturesDelegate (int defaultTextureIndex, int topLeftTypeIndex, int topTypeIndex, int topRightTypeIndex, int leftTypeIndex, int rightTypeIndex, int bottomLeftTypeIndex, int bottomTypeIndex, int bottomRightTypeIndex);

    [Serializable]
    public struct ConnectedTextureConfig
    {
        public bool tl, t, tr, l, r, bl, b, br;
        public Texture2D texture;
        [NonSerialized]
        public int textureIndex, key, slots;
    }

    [CreateAssetMenu (menuName = "Voxel Play/Connected Texture", fileName = "ConnectedTexture", order = 131)]
    public class ConnectedTexture : ScriptableObject
    {

        public enum Side
        {
            AnySide,
            Top,
            Bottom,
            Forward,
            Back,
            Left,
            Right
        }

        [Tooltip("The voxel definition to which this configuration applies")]
        public VoxelDefinition voxelDefinition;

        [Tooltip("The expected neighbour type in the rules below")]
        public VoxelDefinition neighbourDefinition;

        [Tooltip ("The side of the voxel to which these rules apply")]
        public Side side = Side.AnySide;

        public ConnectedTextureConfig [] config;

        int [] solve;
        int neighbourIndex;

        public void Init ()
        {
            if (voxelDefinition == null) return;
            neighbourIndex = neighbourDefinition != null ? neighbourDefinition.index : voxelDefinition.index;
            if (side == Side.Top || side == Side.AnySide) voxelDefinition.customTextureProviderTop = ResolveTexture;
            if (side == Side.Bottom || side == Side.AnySide) voxelDefinition.customTextureProviderBottom = ResolveTexture;
            if (side == Side.Back || side == Side.AnySide) voxelDefinition.customTextureProviderBack = ResolveTexture;
            if (side == Side.Forward || side == Side.AnySide) voxelDefinition.customTextureProviderForward = ResolveTexture;
            if (side == Side.Left || side == Side.AnySide) voxelDefinition.customTextureProviderLeft = ResolveTexture;
            if (side == Side.Right || side == Side.AnySide) voxelDefinition.customTextureProviderRight = ResolveTexture;

            ComputeMatchesMatrix ();
        }

        private void OnValidate ()
        {
            if (neighbourDefinition == null) neighbourDefinition = voxelDefinition;
            ComputeMatchesMatrix ();
        }


        void ComputeMatchesMatrix ()
        {
            if (solve == null || solve.Length == 0) {
                solve = new int [256];
            }

            if (config == null) return;

            for (int k=0;k<solve.Length;k++) {
                solve [k] = 0;
            }

            for (int k = 0; k < config.Length; k++) {
                int key = 0;
                int slots = 0;
                if (config [k].tl) { key += 1; slots++; }
                if (config [k].t) { key += 2; slots++; }
                if (config [k].tr) { key += 4; slots++; }
                if (config [k].l) { key += 8; slots++; }
                if (config [k].r) { key += 16; slots++; }
                if (config [k].bl) { key += 32; slots++; }
                if (config [k].b) { key += 64; slots++; }
                if (config [k].br) { key += 128; slots++; }
                config [k].key = key;
                config [k].slots = slots;
                solve [key] = config [k].textureIndex;
            }

            List<ConnectedTextureConfig> sortedConfig = new List<ConnectedTextureConfig> (config);
            sortedConfig.Sort (comparer);

            // Fill combinations
            for (int k = 0; k < sortedConfig.Count; k++) {
                ConnectedTextureConfig c = sortedConfig [k];
                for (int j = 1; j < 256; j++) {
                    if (solve [j] == 0 && (j & c.key) == c.key) {
                        solve [j] = c.textureIndex;
                    }
                }
            }
        }

        int comparer (ConnectedTextureConfig c1, ConnectedTextureConfig c2)
        {
            if (c1.slots > c2.slots) {
                return -1;
            }
            if (c1.slots < c2.slots) {
                return 1;
            }
            return 0;
        }

        public int ResolveTexture (int defaultTextureIndex, int topLeftTypeIndex, int topTypeIndex, int topRightTypeIndex, int leftTypeIndex, int rightTypeIndex, int bottomLeftTypeIndex, int bottomTypeIndex, int bottomRightTypeIndex)
        {
            int key = 0;
            if (topLeftTypeIndex == neighbourIndex) key += 1;
            if (topTypeIndex == neighbourIndex) key += 2;
            if (topRightTypeIndex == neighbourIndex) key += 4;
            if (leftTypeIndex == neighbourIndex) key += 8;
            if (rightTypeIndex == neighbourIndex) key += 16;
            if (bottomLeftTypeIndex == neighbourIndex) key += 32;
            if (bottomTypeIndex == neighbourIndex) key += 64;
            if (bottomRightTypeIndex == neighbourIndex) key += 128;
            return solve [key] != 0 ? solve [key] : defaultTextureIndex;
        }

    }

    public partial class VoxelDefinition : ScriptableObject
    {
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderBack;
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderForward;
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderTop;
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderBottom;
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderLeft;
        [NonSerialized] public ConnectedTexturesDelegate customTextureProviderRight;
    }
}

