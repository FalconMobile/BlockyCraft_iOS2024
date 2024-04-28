using UnityEngine;

namespace VoxelPlay
{

    static class ShaderParams
    {
        public static int TintColorArray = Shader.PropertyToID ("_TintColor");
        public static int PositionsArray = Shader.PropertyToID ("_Positions");
        public static int ColorsAndLightArray = Shader.PropertyToID ("_ColorsAndLight");
        public static int RotationsArray = Shader.PropertyToID ("_Rotations");
        public static int VoxelLight = Shader.PropertyToID ("_VoxelLight");
        public static int GlobalEmissionIntensity = Shader.PropertyToID ("_VPEmissionIntensity");
        public static int FlashDelay = Shader.PropertyToID ("_FlashDelay");
        public static int TexSides = Shader.PropertyToID ("_TexSides");
        public static int TexBottom = Shader.PropertyToID ("_TexBottom");
        public static int Color = Shader.PropertyToID ("_Color");
        public static int SeeThroughData = Shader.PropertyToID ("_VPSeeThroughData");
        public static int AnimSeed = Shader.PropertyToID ("_AnimSeed");
        public static int MainTex = Shader.PropertyToID ("_MainTex");
    }

}