
namespace VoxelPlay
{

    public struct TextureProviderSettings
    {
        public int textureSize;
        public float textureScale;
        public bool enableNormalMap;
        public bool enableReliefMap;

        public static bool operator == (TextureProviderSettings s1, TextureProviderSettings s2)
        {
            return s1.textureScale == s2.textureScale &&  s1.textureSize == s2.textureSize && s1.enableNormalMap == s2.enableNormalMap && s1.enableReliefMap == s2.enableReliefMap;
        }
        public static bool operator != (TextureProviderSettings s1, TextureProviderSettings s2)
        {
            return s1.textureScale != s2.textureScale || s1.textureSize != s2.textureSize || s1.enableNormalMap != s2.enableNormalMap || s1.enableReliefMap != s2.enableReliefMap;
        }

        public override bool Equals (object obj)
        {
            return base.Equals (obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

    }

}