using System;
using UnityEngine;

namespace VoxelPlay {

	public abstract class VoxelPlayTerrainGenerator : ScriptableObject { 

        protected const int ONE_Y_ROW = VoxelPlayEnvironment.ONE_Y_ROW;
        protected const int ONE_Z_ROW = VoxelPlayEnvironment.ONE_Z_ROW;

        /// <summary>
        /// The maximum height allowed by the terrain generator (usually equals to 255)
        /// </summary>
        [Header ("Terrain Parameters")]
		public float maxHeight = 255;

		public float minHeight = -32;

		public int waterLevel = 25;

		[NonSerialized]
		protected VoxelPlayEnvironment env;

		[NonSerialized]
		protected WorldDefinition world;

		/// <summary>
		/// Resets any cached data and reload info
		/// </summary>
		protected abstract void Init ();

		/// <summary>
		/// Gets the altitude and moisture.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude (0..1) range.</param>
		/// <param name="moisture">Moisture (0..1) range.</param>
		public abstract void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture);

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position"
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		public abstract bool PaintChunk (VoxelChunk chunk);

		/// <summary>
		/// Returns true if the terrain generator is ready to be used. Call Initialize() otherwise.
		/// </summary>
		[NonSerialized]
		public bool isInitialized;



		/// <summary>
		/// Use this method to initialize the terrain generator
		/// </summary>
		public void Initialize () {
			env = VoxelPlayEnvironment.instance;
			if (env == null)
				return;
			world = env.world;
			if (world == null)
				return;

			env.waterLevel = waterLevel;
			Init ();
			isInitialized = true;
		}

	}

}