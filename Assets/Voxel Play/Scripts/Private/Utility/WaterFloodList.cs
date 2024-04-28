using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {
				
	public class WaterFloodList {

		const int WATER_FLOOD_CAPACITY = 2048;

		public struct WaterFloodNode {
			public Vector3d position;
			public int prev, next;
			public float spreadNext;
			public int lifeTime;
			public VoxelDefinition waterVoxel;
		}

		public WaterFloodNode[] nodes;
		public int root = -1;
		public int last = -1;


		HashSet<Vector3d> waterFloodPositions;


		public WaterFloodList () {
			nodes = new WaterFloodNode[WATER_FLOOD_CAPACITY];
			waterFloodPositions = new HashSet<Vector3d> ();
		}

		public void Clear () {
			root = -1;
			last = -1;
			waterFloodPositions.Clear ();
		}

		public void Add (ref Vector3d position, VoxelDefinition waterVoxel, int lifeTime) {
 			if (waterFloodPositions.Contains (position))
				return;

			for (int i = last + 1; i < last + WATER_FLOOD_CAPACITY; i++) {
				int k = i % WATER_FLOOD_CAPACITY;
				if (nodes [k].lifeTime <= 0) {
					waterFloodPositions.Add (position);
					nodes [k].position = position;
					nodes [k].lifeTime = lifeTime;
					nodes [k].waterVoxel = waterVoxel;
					nodes [k].prev = last;
					nodes [k].next = -1;
					if (last != -1) {
						nodes [last].next = k;
					}
					last = k;
					if (root == -1) {
						root = k;
					}
					SetNextSpreadTime (k);
					return;
				}
			}
		}

		public void SetNextSpreadTime(int index) {
			nodes [index].spreadNext = Time.time + nodes [index].waterVoxel.spreadDelay + Random.value * nodes [index].waterVoxel.spreadDelayRandom;
		}

		public int RemoveAt (int index) {
			waterFloodPositions.Remove (nodes [index].position);
			nodes [index].lifeTime = 0;
			int prev = nodes [index].prev;
			int next = nodes [index].next;
			if (prev != -1) {
				nodes [prev].next = next;
			}
			if (next != -1) {
				nodes [next].prev = prev;
			}
			if (root == index) {
				root = next;
			}
			if (last == index) {
				last = prev;
			}
			return next;
		}


	}

}