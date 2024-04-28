using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    // Fallback terrain generator for empty worlds
    public class NullTerrainGenerator : VoxelPlayTerrainGenerator
    {

        protected override void Init () { 
        }

        public override void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture) { 
            altitude = moisture = 0;
        }

        public override bool PaintChunk (VoxelChunk chunk) { 
            chunk.isAboveSurface = true;
            return false;
        }

    }

}