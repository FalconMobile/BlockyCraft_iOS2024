
namespace VoxelPlay {

	/// <summary>
	/// A voxel index represents the location of a voxel in the world
	/// </summary>
	public struct VoxelIndex {
		
		/// <summary>
		/// Position in world space.
		/// </summary>
		public Vector3d position;

		/// <summary>
		/// The chunk to which this voxel belongs to
		/// </summary>
		public VoxelChunk chunk;

		/// <summary>
		/// The index of this voxel in the chunk.voxels[] array
		/// </summary>
		public int voxelIndex;

		/// <summary>
		/// The distance to the center (sqr distance) of the given position to GetVoxelIndices call.
		/// </summary>
		public float sqrDistance;

		/// <summary>
		/// The damage applied to a voxel. Used only with VoxelDamage methods.
		/// </summary>
		public int damageTaken;

		/// <summary>
		/// Returns the voxel definition of this voxel
		/// </summary>
		public VoxelDefinition type {
			get {
                if ((object)chunk != null && voxelIndex >= 0) {
                    return chunk.voxels[voxelIndex].type;
                }
                return null;
            }
		}

		/// <summary>
		/// Returns the voxel definition (integer index) in the env.voxelDefinitions array for this voxel
		/// </summary>
		/// <value>The index of the type.</value>
		public int typeIndex {
			get {
                if ((object)chunk != null && voxelIndex >= 0 ) {
                    return chunk.voxels[voxelIndex].typeIndex;
                }
                return 0;
            }
		}

		public static VoxelIndex Null = new VoxelIndex ();
	}

}