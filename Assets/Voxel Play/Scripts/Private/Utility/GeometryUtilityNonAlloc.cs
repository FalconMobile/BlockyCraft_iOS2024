using UnityEngine;


namespace VoxelPlay {
				
	public static class GeometryUtilityNonAlloc {
								
		public static bool TestPlanesAABB(Vector3[] planesNormals, float[] planesDistances, ref Vector3 boundsMin, ref Vector3 boundsMax) {
			Vector3 vmin;

			for (int planeIndex = 0; planeIndex < planesNormals.Length; planeIndex++) {
				var normal = planesNormals[planeIndex];

				// X axis
				if (normal.x < 0) {
					vmin.x = boundsMin.x;
				} else {
					vmin.x = boundsMax.x;
				}

				// Y axis
				if (normal.y < 0) {
					vmin.y = boundsMin.y;
				} else {
					vmin.y = boundsMax.y;
				}

				// Z axis
				if (normal.z < 0) {
					vmin.z = boundsMin.z;
				} else {              
					vmin.z = boundsMax.z;
				}

				var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
				if (dot1 + planesDistances[planeIndex] < 0) {
					return false;
				}
			}

			return true;
		}

		public static bool TestPlanesAABB (Vector3 [] planesNormals, float [] planesDistances, ref Vector3d boundsMin, ref Vector3d boundsMax)
		{
			Vector3d vmin;

			for (int planeIndex = 0; planeIndex < planesNormals.Length; planeIndex++) {
				var normal = planesNormals [planeIndex];

				// X axis
				if (normal.x < 0) {
					vmin.x = boundsMin.x;
				} else {
					vmin.x = boundsMax.x;
				}

				// Y axis
				if (normal.y < 0) {
					vmin.y = boundsMin.y;
				} else {
					vmin.y = boundsMax.y;
				}

				// Z axis
				if (normal.z < 0) {
					vmin.z = boundsMin.z;
				} else {
					vmin.z = boundsMax.z;
				}

				var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
				if (dot1 + planesDistances [planeIndex] < 0) {
					return false;
				}
			}

			return true;
		}



	}
}