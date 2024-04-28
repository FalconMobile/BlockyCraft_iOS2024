using System;
using UnityEngine;

namespace VoxelPlay
{

    [Serializable]
    public struct BiomeZone
    {
        [HideInInspector, Obsolete ("Use altitudeMin in world space")]
        public float elevationMin;

        [HideInInspector, Obsolete ("Use altitudeMin in world space")]
        public float elevationMax;

        public float altitudeMin;
        public float altitudeMax;

        [Range (0, 1f)]
        public float moistureMin;
        [Range (0, 1f)]
        public float moistureMax;

        [NonSerialized]
        public BiomeDefinition biome;
    }

    [Serializable]
    public struct BiomeTree
    {
        public ModelDefinition tree;
        public float probability;
    }

    [Serializable]
    public struct BiomeVegetation
    {
        public VoxelDefinition vegetation;
        public float probability;
    }

    [Serializable]
    public struct BiomeOre
    {
        public VoxelDefinition ore;
        [Range (0, 1)]
        [Tooltip ("Per chunk minimum probability. This min probability should start at the max value of any previous ore so all probabilities stack up.")]
        public float probabilityMin;
        [Range (0, 1)]
        [Tooltip ("Per chunk maximum probability")]
        public float probabilityMax;
        [Tooltip("Min depth from surface")]
        public int depthMin;
        [Tooltip("Max depth from surface. Required.")]
        public int depthMax;
        [Tooltip ("Min size of vein")]
        public int veinMinSize;
        [Tooltip ("Max size of vein")]
        public int veinMaxSize;
        [Tooltip ("Per chunk minimum number of veins")]
        public int veinsCountMin;
        [Tooltip ("Per chunk maximum number of veins")]
        public int veinsCountMax;
    }


    [CreateAssetMenu (menuName = "Voxel Play/Biome Definition", fileName = "BiomeDefinition", order = 100)]
    [HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000001913-biomes")]
    public partial class BiomeDefinition : ScriptableObject
    {

        [Header ("Biome Settings")]
        public BiomeZone [] zones;

#if UNITY_EDITOR
        // Used by biome map explorer
        [NonSerialized]
        public int biomeMapOccurrences;

        /// <summary>
        /// If this biome is visible in the biome explorer
        /// </summary>
        public bool showInBiomeMap = true;
#endif

        public Color biomeMapColor;

        [NonSerialized]
        public Color biomeMapColorTemp;

        [Header ("Terrain Voxels")]
        public VoxelDefinition voxelTop;
        public VoxelDefinition voxelDirt;
        public BiomeOre [] ores;

        [Header ("Trees")]
        [Range (0, 0.05f)]
        public float treeDensity = 0.02f;
        public BiomeTree [] trees;

        [Header ("Vegetation")]
        [Range (0, 1)]
        public float vegetationDensity = 0.05f;
        public BiomeVegetation [] vegetation;

        [Header ("Underwater Vegetation")]
        [Range (0, 1)]
        public float underwaterVegetationDensity = 0.05f;
        public BiomeVegetation [] underwaterVegetation;

        private void Awake ()
        {
            ValidateSettings ();
        }

        public void ValidateSettings ()
        {

            if (voxelDirt != null && voxelDirt != voxelTop) {
                voxelDirt.navigatable = true;
            }

            if (ores == null) {
                ores = new BiomeOre [0];
            }
            if (trees == null) {
                trees = new BiomeTree [0];
            }
            if (vegetation == null) {
                vegetation = new BiomeVegetation [0];
            }
            if (underwaterVegetation == null) {
                underwaterVegetation = new BiomeVegetation [0];
            }

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            float maxAltitude = 255;
            if (env.world != null && env.world.terrainGenerator != null) {
                maxAltitude = env.world.terrainGenerator.maxHeight;
            }
#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
            if (zones != null) {
                for (int z = 0; z < zones.Length; z++) {
                    BiomeZone zone = zones [z];
                    zone.biome = this;
                    if (zone.elevationMin == 0 && zone.elevationMax == 0) zone.elevationMax = 1;

                    if (zone.altitudeMin == 0 && zone.altitudeMax == 0) {
                        // migrate to world space values
                        zone.altitudeMin = zone.elevationMin * maxAltitude;
                        zone.altitudeMax = zone.elevationMax * maxAltitude;
                    }

                    if (zone.moistureMin == 0 && zone.moistureMax == 0) zone.moistureMax = 1;
                    zones [z] = zone;
                }
            }
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos

            for (int v = 0; v < vegetation.Length; v++) {
                if (vegetation [v].vegetation == null) {
                    vegetation [v].vegetation = env.defaultVoxel;
                }
            }

            for (int v = 0; v < underwaterVegetation.Length; v++) {
                if (underwaterVegetation [v].vegetation == null) {
                    underwaterVegetation [v].vegetation = env.defaultVoxel;
                }
            }

        }
    }
}