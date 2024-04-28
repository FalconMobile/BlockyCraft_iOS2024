using UnityEngine;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        void InitTileRules ()
        {

            // Add tile rules
            ConnectedVoxel [] trs = Resources.LoadAll<ConnectedVoxel> ("");
            for (int k = 0; k < trs.Length; k++) {
                ConnectedVoxel tr = trs [k];
                tr.Init (this);
            }
        }

    }


}
