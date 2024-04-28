using UnityEngine;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        void InitConnectedTextures ()
        {

            // Add connected textures
            ConnectedTexture [] ctt = Resources.LoadAll<ConnectedTexture> ("");
            for (int k = 0; k < ctt.Length; k++) {
                ConnectedTexture ct = ctt [k];
                VoxelDefinition vd = ctt [k].voxelDefinition;
                if (vd == null || vd.index == 0) continue;
                for (int j = 0; j < ct.config.Length; j++) {
                    ct.config [j].textureIndex = mainTextureProvider.AddTexture (ct.config [j].texture, null, null, null);
                }
                ct.Init ();
            }
        }

    }


}
